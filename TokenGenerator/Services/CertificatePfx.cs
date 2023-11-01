using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class CertificatePfx : ICertificateService
    {
        private X509Certificate2 apiTokenSigningCertificate;
        private X509Certificate2 consentTokenSigningCertificate;

        private const string ApiTokenSigningCertPfxPath = "Certificates/apitoken.pfx";
        private const string ApiTokenSigningCertPassword = "apitoken";
        private const string ConsentTokenSigningCertPfxPath = "Certificates/consenttoken.pfx";
        private const string ConsentTokenSigningCertPassword = "consenttoken";

        public async Task<X509Certificate2> GetApiTokenSigningCertificate(string _)
        {
            if (apiTokenSigningCertificate == null)
            {
                string fullPath = Path.Combine(Environment.GetEnvironmentVariable("ApplicationRootPath") ?? string.Empty, ApiTokenSigningCertPfxPath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Expected to find API token signing certificate file at '" + fullPath + "'");
                }

                apiTokenSigningCertificate = new X509Certificate2(fullPath, ApiTokenSigningCertPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return await Task.FromResult(apiTokenSigningCertificate);
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate(string _)
        {
            if (consentTokenSigningCertificate == null)
            {
                string fullPath = Path.Combine(Environment.GetEnvironmentVariable("ApplicationRootPath") ?? string.Empty, ConsentTokenSigningCertPfxPath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Expected to find consent token signing certificate file at '" + fullPath + "'");
                }

                consentTokenSigningCertificate = new X509Certificate2(ConsentTokenSigningCertPfxPath, ConsentTokenSigningCertPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return await Task.FromResult(consentTokenSigningCertificate);
        }

        public Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment)
        {
            throw new NotImplementedException();
        }
    }
}
