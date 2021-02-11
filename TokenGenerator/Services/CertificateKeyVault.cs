namespace TokenGenerator.Services
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Microsoft.Extensions.Options;

    public class CertificateKeyVault : ICertificateService
    {
        private readonly Settings settings;

        private SecretClient secretClient = null;
        private X509Certificate2 apiTokenSigningCertificate = null;
        private X509Certificate2 consentTokenSigningCertificate = null;

        public CertificateKeyVault(IOptions<Settings> settings)
        {
            this.settings = settings.Value;
        }

        public async Task<X509Certificate2> GetApiTokenSigningCertificate()
        {
            if (apiTokenSigningCertificate == null)
            {
                var secretClient = GetSecretClient();
                var certWithPrivateKey = await secretClient.GetSecretAsync(settings.ApiTokenSigningCertName);

                apiTokenSigningCertificate =  new X509Certificate2(Convert.FromBase64String(certWithPrivateKey.Value.Value), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return apiTokenSigningCertificate;
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate()
        {
            if (consentTokenSigningCertificate == null)
            {
                var secretClient = GetSecretClient();
                var certWithPrivateKey = await secretClient.GetSecretAsync(settings.ApiTokenSigningCertName);
                consentTokenSigningCertificate =  new X509Certificate2(Convert.FromBase64String(certWithPrivateKey.Value.Value), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return consentTokenSigningCertificate;
        }

        private SecretClient GetSecretClient()
        {
            if (secretClient == null)
            {
                var kvUri = $"https://{settings.KeyVaultName}.vault.azure.net";
                secretClient = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            }

            return secretClient;
        }
    }
}
