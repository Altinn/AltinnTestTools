using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetEnterpriseUserToken(IOptions<Settings> settings, IToken tokenHelper, IRequestValidator requestValidator, IAuthorization authorization)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetEnterpriseUserToken))]
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
        requestValidator.ValidateQueryParam("org", false, tokenHelper.IsValidIdentifier, out var org);
        requestValidator.ValidateQueryParam("orgNo", true, tokenHelper.IsValidOrgNo, out var orgNo);
        requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out var supplierOrgNo);
        requestValidator.ValidateQueryParam("partyId", true, uint.TryParse, out uint partyId);
        requestValidator.ValidateQueryParam("userId", true, uint.TryParse, out uint userId);
        requestValidator.ValidateQueryParam("partyuuid", false, Guid.TryParse, out Guid partyUuid);
        requestValidator.ValidateQueryParam("userName", true, tokenHelper.IsValidIdentifier, out var userName);
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);
        requestValidator.ValidateQueryParam("delegationSource", false, tokenHelper.IsValidUri, out var delegationSource);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        var token = await tokenHelper.GetEnterpriseUserToken(env, scopes, org, orgNo, supplierOrgNo, partyId, userId, userName, ttl, delegationSource, partyUuid);

        if (!string.IsNullOrEmpty(req.Query["dump"]))
        {
            return new OkObjectResult(tokenHelper.Dump(token));
        }

        return new OkObjectResult(token);
    }
}
