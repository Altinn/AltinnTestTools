using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using TokenGenerator.Helpers;

namespace TokenGenerator
{
    public class GetEnterpriseToken
    {
        private readonly Settings config;

        public GetEnterpriseToken(IOptions<Settings> configurationItems)
        {
            config = configurationItems.Value;
        }

        [FunctionName(nameof(GetEnterpriseToken))]
        public ActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            var rh = new RequestHelper(req);

            rh.ValidateQueryParam("scopes", true, TokenHelper.TryParseScopes, out string[] scopes);
            rh.ValidateQueryParam("org", true, TokenHelper.IsValidIdentifier, out string org);
            rh.ValidateQueryParam("orgNo", true, TokenHelper.IsValidOrgNo, out string orgNo);
            rh.ValidateQueryParam("supplierOrgNo", false, TokenHelper.IsValidOrgNo, out string supplierOrgNo);
            rh.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (rh.Errors.Count > 0)
            {
                 return new BadRequestObjectResult(rh.Errors);
            }
            
            string token = TokenHelper.GetEnterpriseToken(scopes, org, orgNo, supplierOrgNo, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(TokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
