using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenGenerator.Helpers;

namespace TokenGenerator
{
    public class GetPersonalToken
    {
        private readonly Settings config;

        public GetPersonalToken(IOptions<Settings> configurationItems)
        {
            config = configurationItems.Value;
        }

        [FunctionName(nameof(GetPersonalToken))]
        public ActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            var rh = new RequestHelper(req);

            rh.ValidateQueryParam("scopes", true, TokenHelper.TryParseScopes, out string[] scopes);
            rh.ValidateQueryParam("userid", true, uint.TryParse, out uint userId);
            rh.ValidateQueryParam("partyId", true, uint.TryParse, out uint partyId);
            rh.ValidateQueryParam("pid", true, TokenHelper.IsValidPid, out string pid);
            rh.ValidateQueryParam("authlvl", false, TokenHelper.IsValidAuthLvl, out string authLvl, "3");
            rh.ValidateQueryParam("consumerOrgNo", false, TokenHelper.IsValidAuthLvl, out string consumerOrgNo, "991825827");
            rh.ValidateQueryParam("username", false, TokenHelper.IsValidIdentifier, out string userName, "");
            rh.ValidateQueryParam("client_amr", false, TokenHelper.IsValidIdentifier, out string clientAmr, "virksomhetssertifikat");
            rh.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (rh.Errors.Count > 0)
            {
                 return new BadRequestObjectResult(rh.Errors);
            }

            string token = TokenHelper.GetPersonalToken(scopes, userId, partyId, pid, authLvl, consumerOrgNo, userName, clientAmr, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(TokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
