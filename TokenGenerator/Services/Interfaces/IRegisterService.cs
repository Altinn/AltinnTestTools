using Altinn.Register.Models;
using System.Threading;
using System.Threading.Tasks;

namespace TokenGenerator.Services.Interfaces
{
    public interface IRegisterService
    {
        Task<(bool Success, Party Party)> GetEnvironmentIdentifiers(string env, string pid, string platformAccessToken, string subscriptionKey, CancellationToken cancellationToken);
    }
}