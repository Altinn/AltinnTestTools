namespace TokenGenerator.Services
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using Microsoft.Extensions.Options;

    public class CertificateServiceKeyVault : ICertificateService
    {
        private readonly Settings settings;

        private CertificateClient certificateClient = null;
        private X509Certificate2 apiTokenSigningCertificate = null;
        private X509Certificate2 consentTokenSigningCertificate = null;

        public CertificateServiceKeyVault(IOptions<Settings> settings)
        {
            this.settings = settings.Value;
        }

        public async Task<X509Certificate2> GetApiTokenSigningCertificate()
        {
            if (apiTokenSigningCertificate == null)
            {
                var client = GetCertificateClient();
                var cert = await client.GetCertificateAsync(settings.ApiTokenSigningCertName);
                apiTokenSigningCertificate =  new X509Certificate2(cert.Value.Cer, string.Empty, X509KeyStorageFlags.MachineKeySet);
            }

            return apiTokenSigningCertificate;
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate()
        {
            if (consentTokenSigningCertificate == null)
            {
                var client = GetCertificateClient();
                var cert = await client.GetCertificateAsync(settings.ApiTokenSigningCertName);
                consentTokenSigningCertificate =  new X509Certificate2(cert.Value.Cer, string.Empty, X509KeyStorageFlags.MachineKeySet);
            }

            return consentTokenSigningCertificate;
        }

        private CertificateClient GetCertificateClient()
        {
            if (certificateClient == null)
            {
                var kvUri = $"https://{settings.KeyVaultName}.vault.azure.net";
                certificateClient = new CertificateClient(new Uri(kvUri), new DefaultAzureCredential());
            }

            return certificateClient;
        }
    }
}
