using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetPersonalToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IRandomIdentifier randomIdentifier, IOptions<Settings> settings)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetPersonalToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopePersonal, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, ["altinn:enduser"]);
        requestValidator.ValidateQueryParam("userId", false, uint.TryParse, out uint userId);
        requestValidator.ValidateQueryParam("partyId", false, uint.TryParse, out uint partyId);
        requestValidator.ValidateQueryParam("pid", false, tokenHelper.IsValidPid, out var pid);
        requestValidator.ValidateQueryParam("bulkCount", false, uint.TryParse, out uint bulkCount);
        requestValidator.ValidateQueryParam("authLvl", false, tokenHelper.IsValidAuthLvl, out var authLvl, "3");
        requestValidator.ValidateQueryParam("consumerOrgNo", false, tokenHelper.IsValidPidOrOrgNo, out var consumerOrgNo);
        requestValidator.ValidateQueryParam("partyuuid", false, Guid.TryParse, out Guid partyUuid);
        requestValidator.ValidateQueryParam("userName", false, tokenHelper.IsValidIdentifier, out var userName, "");
        requestValidator.ValidateQueryParam("clientAmr", false, tokenHelper.IsValidIdentifier, out var clientAmr, "virksomhetssertifikat");
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);
        requestValidator.ValidateQueryParam("delegationSource", false, tokenHelper.IsValidUri, out var delegationSource);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        if (bulkCount > 0)
        {
            var randomList = randomIdentifier.GetRandomPersonalIdentifiers(bulkCount);
            var tokenList = await tokenHelper.GetTokenList(randomList, async randomPid =>
                await tokenHelper.GetPersonalToken(req, env, scopes, userId, partyId, randomPid, authLvl, consumerOrgNo, userName, clientAmr, ttl, delegationSource, partyUuid));

            return new OkObjectResult(tokenList);
        }

        pid ??= randomIdentifier.GetRandomPersonalIdentifiers(1).First();
        var token = await tokenHelper.GetPersonalToken(req, env, scopes, userId, partyId, pid, authLvl, consumerOrgNo, userName, clientAmr, ttl, delegationSource, partyUuid);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
