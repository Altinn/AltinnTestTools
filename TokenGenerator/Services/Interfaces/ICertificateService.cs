using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TokenGenerator.Services.Interfaces
{
    public interface ICertificateService
    {
        Task<X509Certificate2> GetApiTokenSigningCertificate(string environment);
        Task<X509Certificate2> GetConsentTokenSigningCertificate(string environment);
        Task<X509Certificate2> GetPlatformAccessTokenSigningCertificate(string environment, string issuer);
    }
}