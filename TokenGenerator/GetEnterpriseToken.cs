using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TokenGenerator.Services;

namespace TokenGenerator
{
    public class GetEnterpriseToken
    {
        private readonly Settings settings;
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;

        public GetEnterpriseToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization)
        {
            this.settings = settings.Value;
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
        }

        [FunctionName(nameof(GetEnterpriseToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize();
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
            requestValidator.ValidateQueryParam("delegation_source", false, tokenHelper.IsValidUri, out string delegationsource);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }
            
            string token = await tokenHelper.GetEnterpriseToken(env, scopes, org, orgNo, supplierOrgNo, ttl, delegationsource);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
