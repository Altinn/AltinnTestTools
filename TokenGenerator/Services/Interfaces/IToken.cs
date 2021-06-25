using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public interface IToken
    {
        Task<string> GetEnterpriseToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl);
        Task<string> GetEnterpriseUserToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint partyId, uint userId, string userName, uint ttl);
        Task<string> GetPersonalToken(string env, string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string client_amr, uint ttl);
        string Dump(string token);
        bool IsValidAuthLvl(string authlvl);
        bool IsValidIdentifier(string identifier);
        bool IsValidOrgNo(string orgNo);
        bool IsValidPid(string pid);
        bool IsValidEnvironment(string env);
        bool TryParseScopes(string input, out string[] scopes);
    }
}