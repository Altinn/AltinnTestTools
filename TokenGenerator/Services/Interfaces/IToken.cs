using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TokenGenerator.Services.Interfaces
{
    public interface IToken
    {
        Task<string> GetEnterpriseToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl, string delegationSource);
        Task<string> GetEnterpriseUserToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint partyId, uint userId, string userName, uint ttl, string delegationSource);
        Task<string> GetSystemUserToken(string env, string[] scopes, string orgNo, string supplierOrgNo, string systemUserOrg, string systemUserId, uint ttl);
        Task<string> GetPersonalToken(string env, string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string clientAmr, uint ttl, string delegationSource);
        Task<string> GetConsentToken(string env, string[] serviceCodes, IQueryCollection queryParameters, Guid authorizationCode, string offeredBy, string coveredBy, string handledBy, uint ttl);
        Task<string> GetPlatformToken(string env, string appClaim, uint ttl);
        Task<string> GetPlatformAccessToken(string env, string appClaim, uint ttl);

        string Dump(string token);
        bool IsValidAuthLvl(string authLvl);
        bool IsValidIdentifier(string identifier);
        bool IsValidDottedIdentifier(string identifier);
        bool IsValidOrgNo(string orgNo);
        bool IsValidPid(string pid);
        bool IsValidPidOrOrgNo(string pidOrOrgNo);
        bool IsValidEnvironment(string env);
        bool IsValidUri(string uriString);
        bool IsValidServiceCodeList(string serviceCodes, out string[] serviceCodeList);
        bool TryParseScopes(string input, out string[] scopes);
    }
}
