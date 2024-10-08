﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Options;

using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
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

        public async Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment, string issuer)
        {
            List<X509Certificate2> certificates = null;
            if (issuer == settings.PlatformAccessTokenIssuerName)
            {
                if (string.IsNullOrEmpty(environment) || settings.EnvironmentsApiTokenDict[environment] == null || settings.PlatformAccessTokenSigningCertNamesDict[environment] == null)
                {
                    throw new ArgumentException("Invalid environment");
                }

                certificates = await GetCertificates(settings.EnvironmentsApiTokenDict[environment], settings.PlatformAccessTokenSigningCertNamesDict[environment]);
            }
            else if (issuer == settings.TtdAccessTokenIssuerName)
            {
                if (string.IsNullOrEmpty(issuer) || settings.EnvironmentsTtdAccessTokenDict[environment] == null || settings.TtdAccessTokenSigningCertNamesDict[environment] == null)
                {
                    throw new ArgumentException("Invalid environment or org issuer");
                }

                certificates = await GetCertificates(settings.EnvironmentsTtdAccessTokenDict[environment], settings.TtdAccessTokenSigningCertNamesDict[environment]);
            }
            else
            {
                throw new ArgumentException("Invalid issuer");
            }

            return GetLatestCertificateWithRolloverDelay(certificates, 1);
        }

        private async Task<List<X509Certificate2>> GetCertificates(string keyVaultName, string certificateName)
        {
            await _semaphore.WaitAsync();

            string cacheKey = string.Concat(keyVaultName, certificateName);

            try
            {
                if (_certificateUpdateTime > DateTime.Now && _certificates.ContainsKey(cacheKey) &&
                    _certificates[cacheKey].Count > 0)
                {
                    return _certificates[cacheKey];
                }

                _certificates[cacheKey] = await GetAllCertificateVersions(keyVaultName, certificateName);

                // Reuse the same list of certificates for 1 hour.
                _certificateUpdateTime = DateTime.Now.AddHours(1);

                _certificates[cacheKey] =
                    _certificates[cacheKey].OrderByDescending(cer => cer.NotBefore).ToList();
                return _certificates[cacheKey];
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

                // To avoid temp files not being cleaned up, see tip #5 at https://paulstovell.com/x509certificate2/
                var file = Path.Combine(Path.GetTempPath(), "altinn-tokengenerator-" + Guid.NewGuid());
                try
                {
                    await File.WriteAllBytesAsync(file, Convert.FromBase64String(secret.Value));
                    certificates.Add(new X509Certificate2(file, (string)null, X509KeyStorageFlags.MachineKeySet));
                }
                finally
                {
                    File.Delete(file);
                }
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
