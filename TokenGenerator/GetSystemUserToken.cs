using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetSystemUserToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetSystemUserToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeEnterpriseUser, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("scopes", false, tokenHelper.TryParseScopes, out string[] scopes, ["altinn:enduser"]);
        requestValidator.ValidateQueryParam("orgNo", false, tokenHelper.IsValidOrgNo, out var orgNo, "991825827");
        requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out var supplierOrgNo);
        requestValidator.ValidateQueryParam("systemUserOrg", false, tokenHelper.IsValidOrgNo, out var systemUserOrg, "991825827");
        requestValidator.ValidateQueryParam("systemUserId", true, Guid.TryParse, out Guid systemUserId);
        requestValidator.ValidateQueryParam("clientId", false, tokenHelper.IsValidIdentifier, out var clientId, Guid.NewGuid().ToString());
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        var token = await tokenHelper.GetSystemUserToken(env, scopes, orgNo, supplierOrgNo, systemUserOrg, systemUserId.ToString(), clientId, ttl);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
