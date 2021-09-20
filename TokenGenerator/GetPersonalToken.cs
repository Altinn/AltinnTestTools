using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetPersonalToken
    {
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;

        public GetPersonalToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization)
        {
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
        }

        [FunctionName(nameof(GetPersonalToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize();
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, new[] { "altinn:enduser" });
            requestValidator.ValidateQueryParam("userId", true, uint.TryParse, out uint userId);
            requestValidator.ValidateQueryParam("partyId", true, uint.TryParse, out uint partyId);
            requestValidator.ValidateQueryParam("pid", true, tokenHelper.IsValidPid, out string pid);
            requestValidator.ValidateQueryParam("authLvl", false, tokenHelper.IsValidAuthLvl, out string authLvl, "3");
            requestValidator.ValidateQueryParam("consumerOrgNo", false, tokenHelper.IsValidPidOrOrgNo, out string consumerOrgNo, "991825827");
            requestValidator.ValidateQueryParam("userName", false, tokenHelper.IsValidIdentifier, out string userName, "");
            requestValidator.ValidateQueryParam("clientAmr", false, tokenHelper.IsValidIdentifier, out string clientAmr, "virksomhetssertifikat");
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);
            requestValidator.ValidateQueryParam("delegationSource", false, tokenHelper.IsValidUri, out string delegationSource);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }

            string token = await tokenHelper.GetPersonalToken(env, scopes, userId, partyId, pid, authLvl, consumerOrgNo, userName, clientAmr, ttl, delegationSource);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
