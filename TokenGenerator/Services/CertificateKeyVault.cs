using System.Linq;
using System.Threading;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Azure.Identity;

    using Microsoft.Extensions.Options;

    public class CertificateKeyVault : ICertificateService
    {
        private readonly Settings settings;

        private readonly Dictionary<string, List<X509Certificate2>> _certificates = new Dictionary<string, List<X509Certificate2>>();
        private DateTime _certificateUpdateTime = DateTime.UtcNow;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public CertificateKeyVault(IOptions<Settings> settings)
        {
            this.settings = settings.Value;
        }

        public async Task<X509Certificate2> GetApiTokenSigningCertificate(string environment)
        {
            if (string.IsNullOrEmpty(environment) || settings.EnvironmentsApiTokenDict[environment] == null || settings.ApiTokenSigningCertNamesDict[environment] == null)
            {
                throw new ArgumentException("Invalid environment");
            }

            var certificates = await GetCertificates(settings.EnvironmentsApiTokenDict[environment],
                settings.ApiTokenSigningCertNamesDict[environment]);

            return GetLatestCertificateWithRolloverDelay(certificates, 1);
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment)
        {

            if (string.IsNullOrEmpty(environment) || settings.EnvironmentsConsentTokenDict[environment] == null || settings.ConsentTokenSigningCertNamesDict[environment] == null)
            {
                throw new ArgumentException("Invalid environment");
            }
            var certificates = await GetCertificates(settings.EnvironmentsConsentTokenDict[environment],
                settings.ConsentTokenSigningCertNamesDict[environment]);

            return GetLatestCertificateWithRolloverDelay(certificates, 1);
        }

        private async Task<List<X509Certificate2>> GetCertificates(string keyVaultName, string certificateName)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_certificateUpdateTime > DateTime.Now && _certificates.ContainsKey(keyVaultName) &&
                    _certificates[keyVaultName].Count > 0)
                {
                    return _certificates[keyVaultName];
                }

                if (!_certificates.ContainsKey(keyVaultName))
                {
                    _certificates[keyVaultName] = new List<X509Certificate2>();
                }

                List<X509Certificate2> certificates = await GetAllCertificateVersions(keyVaultName, certificateName);
                _certificates[keyVaultName].AddRange(certificates);

                // Reuse the same list of certificates for 1 hour.
                _certificateUpdateTime = DateTime.Now.AddHours(1);

                _certificates[keyVaultName] =
                    _certificates[keyVaultName].OrderByDescending(cer => cer.NotBefore).ToList();
                return _certificates[keyVaultName];
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<X509Certificate2>> GetAllCertificateVersions(string keyVaultName, string certificateName)
        {
            List<X509Certificate2> certificates = new List<X509Certificate2>();

            var certificateClient = GetCertificateClient(keyVaultName);
            var secretClient = GetSecretClient(keyVaultName);


            await foreach (var cert in certificateClient.GetPropertiesOfCertificateVersionsAsync(certificateName))
            {
                if (cert.Enabled == false || cert.ExpiresOn < DateTime.UtcNow)
                {
                    continue;
                }

                KeyVaultCertificate certificate = await certificateClient.GetCertificateVersionAsync(certificateName, cert.Version);

                // Parse the secret ID and version to retrieve the private key.
                var segments = certificate.SecretId.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length != 3)
                {
                    // Should not happen, but just skip if it does
                    continue;
                }

                var secretName = segments[1];
                var secretVersion = segments[2];

                KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName, secretVersion);

                if (!"application/x-pkcs12".Equals(secret.Properties.ContentType,
                        StringComparison.InvariantCultureIgnoreCase)) continue;

                certificates.Add(new X509Certificate2(Convert.FromBase64String(secret.Value)));
            }
            return certificates;
        }

        private X509Certificate2 GetLatestCertificateWithRolloverDelay(
            List<X509Certificate2> certificates, int rolloverDelayHours)
        {
            // First limit the search to just those certificates that have existed longer than the rollover delay.
            var rolloverCutoff = DateTime.Now.AddHours(-rolloverDelayHours);
            var potentialCerts =
                certificates.Where(c => c.NotBefore < rolloverCutoff).ToList();

            // If no certs could be found, then widen the search to any usable certificate.
            if (!potentialCerts.Any())
            {
                potentialCerts = certificates.Where(c => c.NotBefore < DateTime.Now).ToList();
            }

            // Of the potential certs, return the newest one.
            return potentialCerts
                .OrderByDescending(c => c.NotBefore)
                .FirstOrDefault();
        }

        private SecretClient GetSecretClient(string keyVaultName)
        {
            var kvUri = $"https://{keyVaultName}.vault.azure.net";
            return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        }

        private CertificateClient GetCertificateClient(string keyVaultName)
        {
            var kvUri = $"https://{keyVaultName}.vault.azure.net";
            return new CertificateClient(new Uri(kvUri), new DefaultAzureCredential());
        }
    }
}
