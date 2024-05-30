using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetSystemUserToken
    {
        private readonly Settings settings;
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;

        public GetSystemUserToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization)
        {
            this.settings = settings.Value;
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
        }

        [FunctionName(nameof(GetSystemUserToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeEnterpriseUser);
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, new string[] { "altinn:enduser" });
            requestValidator.ValidateQueryParam("orgNo", false, tokenHelper.IsValidOrgNo, out string orgNo, "991825827");
            requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out string supplierOrgNo);
            requestValidator.ValidateQueryParam("systemUserOrg", false, tokenHelper.IsValidOrgNo, out string systemUserOrg, "991825827");
            requestValidator.ValidateQueryParam("systemUserId", true, tokenHelper.IsValidIdentifier, out string systemUserId);
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }

            string token = await tokenHelper.GetSystemUserToken(env, scopes, orgNo, supplierOrgNo, systemUserOrg, systemUserId, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
