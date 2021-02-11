namespace TokenGenerator.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class CertificatePfx : ICertificateService
    {
        private X509Certificate2 apiTokenSigningCertificate = null;
        private X509Certificate2 consentTokenSigningCertificate = null;

        private const string API_TOKEN_SIGNING_CERT_PFX_PATH = "Certificates/apitoken.pfx";
        private const string API_TOKEN_SIGNING_CERT_PASSWORD = "apitoken";
        private const string CONSENT_TOKEN_SIGNING_CERT_PFX_PATH = "Certificates/consenttoken.pfx";
        private const string CONSENT_TOKEN_SIGNING_CERT_PASSWORD = "consenttoken";

        public async Task<X509Certificate2> GetApiTokenSigningCertificate()
        {
            if (apiTokenSigningCertificate == null)
            {
                string fullPath = Path.Combine(Environment.GetEnvironmentVariable("ApplicationRootPath"), API_TOKEN_SIGNING_CERT_PFX_PATH);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Expected to find API token signing certificate file at '" + fullPath + "'");
                }

                apiTokenSigningCertificate = new X509Certificate2(fullPath, API_TOKEN_SIGNING_CERT_PASSWORD,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return await Task.FromResult(apiTokenSigningCertificate);
        }

        public async Task<X509Certificate2> GetConsentTokenSigningCertificate()
        {
            if (consentTokenSigningCertificate == null)
            {
                string fullPath = Path.Combine(Environment.GetEnvironmentVariable("ApplicationRootPath"), CONSENT_TOKEN_SIGNING_CERT_PFX_PATH);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Expected to find consent token signing certificate file at '" + fullPath + "'");
                }

                consentTokenSigningCertificate = new X509Certificate2(CONSENT_TOKEN_SIGNING_CERT_PFX_PATH, CONSENT_TOKEN_SIGNING_CERT_PASSWORD,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            return await Task.FromResult(consentTokenSigningCertificate);
        }
    }
}
