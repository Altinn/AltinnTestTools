using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class SelfSignedCertificate : ICertificateService
{
    private readonly Lazy<X509Certificate2> lazyCertificate;

    public SelfSignedCertificate()
    {
        lazyCertificate = new Lazy<X509Certificate2>(GenerateCertificate);
    }

    public Task<X509Certificate2> GetApiTokenSigningCertificate(string environment)
    {
        return Task.FromResult(lazyCertificate.Value);
    }

    public Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment)
    {
        return Task.FromResult(lazyCertificate.Value);
    }

    public Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment)
    {
        return Task.FromResult(lazyCertificate.Value);
    }

    private X509Certificate2 GenerateCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "cn=LocalTestCertificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(5));
    }
}
