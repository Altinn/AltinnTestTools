using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetConsentToken
    {
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;
        private readonly Settings settings;

        public GetConsentToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IOptions<Settings> settings)
        {
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
            this.settings = settings.Value;
        }

        [FunctionName(nameof(GetConsentToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeConsent);
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("serviceCodes", true, tokenHelper.IsValidServiceCodeList, out string[] serviceCodes);
            requestValidator.ValidateQueryParam("authorizationCode", false, Guid.TryParse, out Guid authorizationCode, Guid.NewGuid());
            requestValidator.ValidateQueryParam("offeredBy", true, tokenHelper.IsValidPidOrOrgNo, out string offeredBy);
            requestValidator.ValidateQueryParam("coveredBy", true, tokenHelper.IsValidPidOrOrgNo, out string coveredBy);
            requestValidator.ValidateQueryParam("handledBy", false, tokenHelper.IsValidPidOrOrgNo, out string handledBy);
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 30);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }

            string token = await tokenHelper.GetConsentToken(env, serviceCodes, req.Query, authorizationCode,
                offeredBy, coveredBy, handledBy, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
