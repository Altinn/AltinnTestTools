using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenGenerator.Services;
using System.Threading.Tasks;

namespace TokenGenerator
{
    public class GetPersonalToken
    {
        private readonly Settings settings;
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;

        public GetPersonalToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator)
        {
            this.settings = settings.Value;
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
        }

        [FunctionName(nameof(GetPersonalToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            requestValidator.ValidateQueryParam("scopes", true, tokenHelper.TryParseScopes, out string[] scopes);
            requestValidator.ValidateQueryParam("userid", true, uint.TryParse, out uint userId);
            requestValidator.ValidateQueryParam("partyId", true, uint.TryParse, out uint partyId);
            requestValidator.ValidateQueryParam("pid", true, tokenHelper.IsValidPid, out string pid);
            requestValidator.ValidateQueryParam("authlvl", false, tokenHelper.IsValidAuthLvl, out string authLvl, "3");
            requestValidator.ValidateQueryParam("consumerOrgNo", false, tokenHelper.IsValidAuthLvl, out string consumerOrgNo, "991825827");
            requestValidator.ValidateQueryParam("username", false, tokenHelper.IsValidIdentifier, out string userName, "");
            requestValidator.ValidateQueryParam("client_amr", false, tokenHelper.IsValidIdentifier, out string clientAmr, "virksomhetssertifikat");
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }

            string token = await tokenHelper.GetPersonalToken(scopes, userId, partyId, pid, authLvl, consumerOrgNo, userName, clientAmr, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
