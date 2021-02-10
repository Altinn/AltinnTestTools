using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public interface IToken
    {
        Task<string> GetEnterpriseToken(string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl);
        Task<string> GetPersonalToken(string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string client_amr, uint ttl);
        string Dump(string token);
        bool IsValidAuthLvl(string authlvl);
        bool IsValidIdentifier(string identifier);
        bool IsValidOrgNo(string orgNo);
        bool IsValidPid(string pid);
        bool TryParseScopes(string input, out string[] scopes);
    }
}