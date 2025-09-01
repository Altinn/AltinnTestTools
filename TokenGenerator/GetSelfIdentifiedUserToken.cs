using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetSelfIdentifiedUserToken
    {
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;
        private readonly Settings settings;

        public GetSelfIdentifiedUserToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IOptions<Settings> settings)
        {
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
            this.settings = settings.Value;
        }

        [FunctionName(nameof(GetSelfIdentifiedUserToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopePersonal);
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            var rnd = new Random();

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("userId", true, uint.TryParse, out uint userId);
            requestValidator.ValidateQueryParam("partyId", false, uint.TryParse, out uint partyId, (uint)rnd.Next(5000000, 7000000));
            requestValidator.ValidateQueryParam("partyuuid", false, Guid.TryParse, out Guid partyUuid, Guid.NewGuid());
            requestValidator.ValidateQueryParam("username", false, tokenHelper.IsValidIdentifier, out string username, $"SIUser{rnd.Next(1000, 9999)}");
            requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out uint ttl, 1800);

            if (requestValidator.GetErrors().Count > 0)
            {
                 return new BadRequestObjectResult(requestValidator.GetErrors());
            }

            string token = await tokenHelper.GeSelfIdentifiedUserToken(env, userId, partyId, partyUuid, username, ttl);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
