using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator
{
    public class GetPersonalToken
    {
        private readonly IToken tokenHelper;
        private readonly IRequestValidator requestValidator;
        private readonly IAuthorization authorization;
        private readonly IRandomIdentifier randomIdentifier;
        private readonly Settings settings;

        public GetPersonalToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IRandomIdentifier randomIdentifier, IOptions<Settings> settings)
        {
            this.tokenHelper = tokenHelper;
            this.requestValidator = requestValidator;
            this.authorization = authorization;
            this.randomIdentifier = randomIdentifier;
            this.settings = settings.Value;
        }

        [FunctionName(nameof(GetPersonalToken))]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            ActionResult failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopePersonal);
            if (failedAuthorizationResult != null)
            {
                return failedAuthorizationResult;
            }

            requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out string env);
            requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, new[] { "altinn:enduser" });
            requestValidator.ValidateQueryParam("userId", false, uint.TryParse, out uint userId);
            requestValidator.ValidateQueryParam("partyId", false, uint.TryParse, out uint partyId);
            requestValidator.ValidateQueryParam("pid", false, tokenHelper.IsValidPid, out string pid);
            requestValidator.ValidateQueryParam("bulkCount", false, uint.TryParse, out uint bulkCount);
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

            if (bulkCount > 0)
            {
                var randomList = randomIdentifier.GetRandomPersonalIdentifiers(bulkCount);
                var tokenList = await tokenHelper.GetTokenList(randomList, async randomPid => 
                    await tokenHelper.GetPersonalToken(req, env, scopes, userId, partyId, randomPid, authLvl, consumerOrgNo, userName, clientAmr, ttl, delegationSource));

                return new OkObjectResult(tokenList);
            }
            
            pid ??= randomIdentifier.GetRandomPersonalIdentifiers(1).First();
            string token = await tokenHelper.GetPersonalToken(req, env, scopes, userId, partyId, pid, authLvl, consumerOrgNo, userName, clientAmr, ttl, delegationSource);

            if (!string.IsNullOrEmpty(req.Query["dump"]))
            {
                return new OkObjectResult(tokenHelper.Dump(token));
            }

            return new OkObjectResult(token);
        }
    }
}
