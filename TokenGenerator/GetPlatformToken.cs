using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetPlatformToken(IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization, IOptions<Settings> settings)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetPlatformToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopePlatform, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("app", true, tokenHelper.IsValidDottedIdentifier, out var appClaim);
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        var token = await tokenHelper.GetPlatformToken(env, appClaim, ttl);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
