using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Options;

using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class CertificateKeyVault(IOptions<Settings> settings) : ICertificateService
{
    private readonly Settings settings = settings.Value;

    private readonly Dictionary<string, List<X509Certificate2>> certificates = [];
    private DateTime certificateUpdateTime = DateTime.UtcNow;

    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<X509Certificate2> GetApiTokenSigningCertificate(string environment)
    {
        if (string.IsNullOrEmpty(environment) ||
            !settings.EnvironmentsApiTokenDict.TryGetValue(environment, out var keyVaultName) ||
            !settings.ApiTokenSigningCertNamesDict.TryGetValue(environment, out var certificateName))
        {
            throw new ArgumentException("Invalid environment");
        }

        var certs = await GetCertificates(keyVaultName, certificateName);

        return GetLatestCertificateWithRolloverDelay(certs, keyVaultName, certificateName, 1);
    }

    public async Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment)
    {

        if (string.IsNullOrEmpty(environment) ||
            !settings.EnvironmentsConsentTokenDict.TryGetValue(environment, out var keyVaultName) ||
            !settings.ConsentTokenSigningCertNamesDict.TryGetValue(environment, out var certificateName))
        {
            throw new ArgumentException("Invalid environment");
        }

        var certs = await GetCertificates(keyVaultName, certificateName);

        return GetLatestCertificateWithRolloverDelay(certs, keyVaultName, certificateName, 1);
    }

    public async Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment, string issuer)
    {
        List<X509Certificate2> certs;
        string keyVaultName;
        string certificateName;
        if (issuer == settings.PlatformAccessTokenIssuerName)
        {
            if (string.IsNullOrEmpty(environment) ||
                !settings.EnvironmentsApiTokenDict.TryGetValue(environment, out keyVaultName) ||
                !settings.PlatformAccessTokenSigningCertNamesDict.TryGetValue(environment, out certificateName))
            {
                throw new ArgumentException("Invalid environment");
            }

            certs = await GetCertificates(keyVaultName, certificateName);
        }
        else if (issuer == settings.TtdAccessTokenIssuerName)
        {
            if (string.IsNullOrEmpty(environment) ||
                !settings.EnvironmentsTtdAccessTokenDict.TryGetValue(environment, out keyVaultName) ||
                !settings.TtdAccessTokenSigningCertNamesDict.TryGetValue(environment, out certificateName))
            {
                throw new ArgumentException("Invalid environment or org issuer");
            }

            certs = await GetCertificates(keyVaultName, certificateName);
        }
        else
        {
            throw new ArgumentException("Invalid issuer");
        }

        return GetLatestCertificateWithRolloverDelay(certs, keyVaultName, certificateName, 1);
    }

    private async Task<List<X509Certificate2>> GetCertificates(string keyVaultName, string certificateName)
    {
        await semaphore.WaitAsync();

        var cacheKey = string.Concat(keyVaultName, certificateName);

        try
        {
            if (certificateUpdateTime > DateTime.UtcNow && certificates.ContainsKey(cacheKey) &&
                certificates[cacheKey].Count > 0)
            {
                return certificates[cacheKey];
            }

            certificates[cacheKey] = await GetAllCertificateVersions(keyVaultName, certificateName);

            // Reuse the same list of certificates for 1 hour.
            certificateUpdateTime = DateTime.UtcNow.AddHours(1);

            certificates[cacheKey] =
                [.. certificates[cacheKey].OrderByDescending(cer => cer.NotBefore)];
            return certificates[cacheKey];
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task<List<X509Certificate2>> GetAllCertificateVersions(string keyVaultName, string certificateName)
    {
        List<X509Certificate2> certs = [];

        var certificateClient = GetCertificateClient(keyVaultName);
        var secretClient = GetSecretClient(keyVaultName);


        await foreach (var cert in certificateClient.GetPropertiesOfCertificateVersionsAsync(certificateName))
        {
            if (cert.Enabled == false || cert.ExpiresOn < DateTime.UtcNow)
            {
                continue;
            }

            var certificate = (await certificateClient.GetCertificateVersionAsync(certificateName, cert.Version)).Value;

            // Parse the secret ID and version to retrieve the private key.
            var segments = certificate.SecretId.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                // Should not happen, but just skip if it does
                continue;
            }

            var secretName = segments[1];
            var secretVersion = segments[2];

            var secret = (await secretClient.GetSecretAsync(secretName, secretVersion)).Value;

            if (!"application/x-pkcs12".Equals(secret.Properties.ContentType,
                    StringComparison.InvariantCultureIgnoreCase)) continue;

            certs.Add(X509CertificateLoader.LoadPkcs12(
                Convert.FromBase64String(secret.Value),
                null,
                X509KeyStorageFlags.MachineKeySet));
        }
        return certs;
    }

    private static X509Certificate2 GetLatestCertificateWithRolloverDelay(
        List<X509Certificate2> certs,
        string keyVaultName,
        string certificateName,
        int rolloverDelayHours)
    {
        if (certs.Count == 0)
        {
            throw new InvalidOperationException($"No usable certificate versions found for '{certificateName}' in Key Vault '{keyVaultName}'.");
        }

        // First limit the search to just those certificates that have existed longer than the rollover delay.
        var rolloverCutoff = DateTime.UtcNow.AddHours(-rolloverDelayHours);
        var potentialCerts =
            certs.Where(c => c.NotBefore.ToUniversalTime() < rolloverCutoff).ToList();

        // If no certs could be found, then widen the search to any usable certificate.
        if (!potentialCerts.Any())
        {
            potentialCerts = [.. certs.Where(c => c.NotBefore.ToUniversalTime() < DateTime.UtcNow)];
        }

        if (potentialCerts.Count == 0)
        {
            throw new InvalidOperationException($"No active certificate versions found for '{certificateName}' in Key Vault '{keyVaultName}'.");
        }

        // Of the potential certs, return the newest one.
        return potentialCerts
            .OrderByDescending(c => c.NotBefore)
            .First();
    }

    private static SecretClient GetSecretClient(string keyVaultName)
    {
        var kvUri = $"https://{keyVaultName}.vault.azure.net";
        return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
    }

    private static CertificateClient GetCertificateClient(string keyVaultName)
    {
        var kvUri = $"https://{keyVaultName}.vault.azure.net";
        return new CertificateClient(new Uri(kvUri), new DefaultAzureCredential());
    }
}
