using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetSelfIdentifiedUserToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IOptions<Settings> settings)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetSelfIdentifiedUserToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopePersonal, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        var rnd = new Random();

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("userId", false, uint.TryParse, out uint userId);
        requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, ["altinn:portal/enduser"]);
        requestValidator.ValidateQueryParam("partyId", false, uint.TryParse, out var partyId, (uint)rnd.Next(5000000, 7000000));
        requestValidator.ValidateQueryParam("partyuuid", false, Guid.TryParse, out var partyUuid, Guid.NewGuid());
        requestValidator.ValidateQueryParam("username", false, tokenHelper.IsValidIdentifier, out var username, $"SIUser{rnd.Next(1000, 9999)}");

        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        var token = await tokenHelper.GetSelfIdentifiedUserToken(env, scopes, userId, partyId, partyUuid, username, ttl);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
