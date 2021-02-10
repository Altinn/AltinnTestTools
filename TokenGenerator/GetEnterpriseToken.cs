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

        public GetEnterpriseToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator)
        {
            this.settings = settings.Value;
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
        }

        [FunctionName(nameof(GetEnterpriseToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            requestValidator.ValidateQueryParam("scopes", true, tokenHelper.TryParseScopes, out string[] scopes);
            requestValidator.ValidateQueryParam("org", true, tokenHelper.IsValidIdentifier, out string org);
            requestValidator.ValidateQueryParam("orgNo", true, tokenHelper.IsValidOrgNo, out string orgNo);
            requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out string supplierOrgNo);
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }
            
            string token = await tokenHelper.GetEnterpriseToken(scopes, org, orgNo, supplierOrgNo, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
