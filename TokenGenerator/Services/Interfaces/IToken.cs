using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TokenGenerator.Services.Interfaces
{
    public interface IToken
    {
        Task<string> GetEnterpriseToken(HttpRequest req, string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl, string delegationSource);
        Task<string> GetEnterpriseUserToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint partyId, uint userId, string userName, uint ttl, string delegationSource, Guid partyUuid);
        Task<string> GetSystemUserToken(string env, string[] scopes, string orgNo, string supplierOrgNo, string systemUserOrg, string systemUserId, string clientId, uint ttl);
        Task<string> GetPersonalToken(HttpRequest req, string env, string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string clientAmr, uint ttl, string delegationSource, Guid partyUuid);
        Task<string> GetConsentToken(string env, string[] serviceCodes, IQueryCollection queryParameters, Guid authorizationCode, string offeredBy, string coveredBy, string handledBy, uint ttl);
        Task<string> GetPlatformToken(string env, string appClaim, uint ttl);
        Task<string> GetPlatformAccessToken(string env, string appClaim, uint ttl, string iss = "platform");
        Task<Dictionary<string, string>> GetTokenList(List<string> claimValues, Func<string, Task<string>> getToken);

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
