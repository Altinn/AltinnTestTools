using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenGenerator.Services.Interfaces
{
    public interface IRegisterService
    {
        Task<(bool Success, uint UserId, uint PartyId, Guid PartyUuid)> GetEnvironmentIdentifiers(string env, string pid, string platformAccessToken, string subscriptionKey, CancellationToken cancellationToken);
    }
}