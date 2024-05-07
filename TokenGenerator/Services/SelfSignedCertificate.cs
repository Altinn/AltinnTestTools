using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class SelfSignedCertificate : ICertificateService
{
    private readonly Lazy<Task<X509Certificate2>> _lazyCertificate;

    public SelfSignedCertificate()
    {
        _lazyCertificate = new Lazy<Task<X509Certificate2>>(GenerateCertificate);
    }

    public Task<X509Certificate2> GetApiTokenSigningCertificate(string environment)
    {
        return _lazyCertificate.Value;
    }

    public Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment)
    {
        return _lazyCertificate.Value;
    }

    public Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment)
    {
        return _lazyCertificate.Value;
    }

    private async Task<X509Certificate2> GenerateCertificate()
    {
        using (RSA rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest(
                "cn=LocalTestCertificate",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(5));

            // Save the certificate to a file or keep it in memory
            // For in-memory usage in testing, you might not need to export it
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "yourPfxPassword"), "yourPfxPassword", X509KeyStorageFlags.DefaultKeySet);
        }
    }
}
