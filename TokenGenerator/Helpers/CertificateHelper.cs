using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TokenGenerator.Helpers
{
    public static class CertificateHelper
    {
        private static X509Certificate2 apiTokenSigningCertificate = null;
        private static X509Certificate2 consentTokenSigningCertificate = null;

        private const string API_TOKEN_SIGNING_CERT_PFX_PATH = "certs/apitoken.pfx";
        private const string API_TOKEN_SIGNING_CERT_PASSWORD = "apitoken";

        private const string CONSENT_TOKEN_SIGNING_CERT_PFX_PATH = "certs/consenttoken.pfx";
        private const string CONSENT_TOKEN_SIGNING_CERT_PASSWORD = "apitoken";

        public static X509Certificate2 ApiTokenSigningCertificate
        {
            get
            {
                if (apiTokenSigningCertificate == null)
                {
                    if (!File.Exists(API_TOKEN_SIGNING_CERT_PFX_PATH))
                    {
                        throw new FileNotFoundException("Expected to find API token signing certificate file at '" + API_TOKEN_SIGNING_CERT_PFX_PATH + "'. Current directory: " + Directory.GetCurrentDirectory());
                    }

                    apiTokenSigningCertificate = new X509Certificate2(API_TOKEN_SIGNING_CERT_PFX_PATH, API_TOKEN_SIGNING_CERT_PASSWORD,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                }

                return apiTokenSigningCertificate;
            }
        }

        public static X509Certificate2 ConsentTokenSigningCertificate
        {
            get
            {
                if (consentTokenSigningCertificate == null)
                {

                    if (!File.Exists(CONSENT_TOKEN_SIGNING_CERT_PFX_PATH))
                    {
                        throw new FileNotFoundException("Expected to find consent token signing certificate file at '" + CONSENT_TOKEN_SIGNING_CERT_PFX_PATH + "'. Current directory: " + Directory.GetCurrentDirectory());
                    }

                    consentTokenSigningCertificate = new X509Certificate2(CONSENT_TOKEN_SIGNING_CERT_PFX_PATH, CONSENT_TOKEN_SIGNING_CERT_PASSWORD,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                }

                return consentTokenSigningCertificate;
            }
        }

        /*
        private async Task<X509Certificate2> GetSigningCertificate()
        {
            var client = GetCertificateClient();
            var cert = await client.GetCertificateAsync(config.TokenSigningCertName);
            return new X509Certificate2(cert.Value.Cer, string.Empty, X509KeyStorageFlags.MachineKeySet);
        }

        private CertificateClient GetCertificateClient()
        {
            var kvUri = $"https://{config.KeyVaultName}.vault.azure.net";
            return new CertificateClient(new Uri(kvUri), new DefaultAzureCredential());
        }
        */
    }
}
