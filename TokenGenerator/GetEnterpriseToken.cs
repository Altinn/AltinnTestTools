using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetEnterpriseToken
    {
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;
        private readonly Settings settings;

        public GetEnterpriseToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IOptions<Settings> settings)
        {
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
            this.settings = settings.Value;
        }

        [FunctionName(nameof(GetEnterpriseToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeEnterprise);
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("scopes", true, tokenHelper.TryParseScopes, out string[] scopes);
            requestValidator.ValidateQueryParam("org", true, tokenHelper.IsValidIdentifier, out string org);
            requestValidator.ValidateQueryParam("orgNo", true, tokenHelper.IsValidOrgNo, out string orgNo);
            requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out string supplierOrgNo);
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);
            requestValidator.ValidateQueryParam("delegationSource", false, tokenHelper.IsValidUri, out string delegationSource);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }
            
            string token = await tokenHelper.GetEnterpriseToken(env, scopes, org, orgNo, supplierOrgNo, ttl, delegationSource);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
