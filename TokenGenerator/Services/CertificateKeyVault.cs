using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Microsoft.Extensions.Options;

    public class CertificateKeyVault : ICertificateService
    {
        private readonly Settings settings;

        private readonly Dictionary<string, X509Certificate2> apiTokenSigningCertificates = new Dictionary<string, X509Certificate2>();
        private readonly Dictionary<string, X509Certificate2> consentTokenSigningCertificates = new Dictionary<string, X509Certificate2>();

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

            if (!string.IsNullOrEmpty(environment) && !apiTokenSigningCertificates.ContainsKey(environment))
            {
                var secretClient = GetSecretClient(settings.EnvironmentsApiTokenDict[environment]);
                var certWithPrivateKey = await secretClient.GetSecretAsync(settings.ApiTokenSigningCertNamesDict[environment]);

                apiTokenSigningCertificates[environment] = new X509Certificate2(Convert.FromBase64String(certWithPrivateKey.Value.Value), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return apiTokenSigningCertificates[environment];
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment)
        {

            if (string.IsNullOrEmpty(environment) || settings.EnvironmentsConsentTokenDict[environment] == null || settings.ConsentTokenSigningCertNamesDict[environment] == null)
            {
                throw new ArgumentException("Invalid environment");
            }

            if (!string.IsNullOrEmpty(environment) && !consentTokenSigningCertificates.ContainsKey(environment))
            {
                var secretClient = GetSecretClient(settings.EnvironmentsConsentTokenDict[environment]);
                var certWithPrivateKey = await secretClient.GetSecretAsync(settings.ConsentTokenSigningCertNamesDict[environment]);
                consentTokenSigningCertificates[environment] =  new X509Certificate2(Convert.FromBase64String(certWithPrivateKey.Value.Value), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return consentTokenSigningCertificates[environment];
        }

        private SecretClient GetSecretClient(string keyVaultName)
        {
            var kvUri = $"https://{keyVaultName}.vault.azure.net";
            return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        }
    }
}
