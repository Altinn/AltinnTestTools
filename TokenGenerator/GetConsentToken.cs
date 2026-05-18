using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetConsentToken(
    IToken tokenHelper,
    IRequestValidator requestValidator,
    IAuthorization authorization,
    IOptions<Settings> settings)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetConsentToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeConsent, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("serviceCodes", true, tokenHelper.IsValidServiceCodeList, out string[] serviceCodes);
        requestValidator.ValidateQueryParam("authorizationCode", false, Guid.TryParse, out var authorizationCode, Guid.NewGuid());
        requestValidator.ValidateQueryParam("offeredBy", true, tokenHelper.IsValidPidOrOrgNo, out var offeredBy);
        requestValidator.ValidateQueryParam("coveredBy", true, tokenHelper.IsValidPidOrOrgNo, out var coveredBy);
        requestValidator.ValidateQueryParam("handledBy", false, tokenHelper.IsValidPidOrOrgNo, out var handledBy);
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 30);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        var token = await tokenHelper.GetConsentToken(env, serviceCodes, req.Query, authorizationCode,
            offeredBy, coveredBy, handledBy, ttl);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
