using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator;

public class GetEnterpriseToken(
    IToken tokenHelper,
    IRequestValidator requestValidator,
    IAuthorization authorization,
    IRandomIdentifier randomIdentifier,
    IOptions<Settings> settings)
{
    private readonly Settings settings = settings.Value;

    [Function(nameof(GetEnterpriseToken))]
    public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var failedAuthorizationResult = await authorization.Authorize(settings.AuthorizedScopeEnterprise, req);
        if (failedAuthorizationResult != null)
        {
            return failedAuthorizationResult;
        }

        requestValidator.SetRequest(req);
        requestValidator.ValidateQueryParam("env", true, tokenHelper.IsValidEnvironment, out var env);
        requestValidator.ValidateQueryParam("scopes", true, tokenHelper.TryParseScopes, out string[] scopes);
        requestValidator.ValidateQueryParam("org", false, tokenHelper.IsValidIdentifier, out var org);
        requestValidator.ValidateQueryParam("orgNo", false, tokenHelper.IsValidOrgNo, out var orgNo);
        requestValidator.ValidateQueryParam("bulkCount", false, uint.TryParse, out uint bulkCount);
        requestValidator.ValidateQueryParam("supplierOrgNo", false, tokenHelper.IsValidOrgNo, out var supplierOrgNo);
        requestValidator.ValidateQueryParam<uint>("ttl", false, uint.TryParse, out var ttl, 1800);
        requestValidator.ValidateQueryParam("delegationSource", false, tokenHelper.IsValidUri, out var delegationSource);
        requestValidator.ValidateQueryParam("clientId", false, clientId => !string.IsNullOrEmpty(clientId), out var clientId);

        if (requestValidator.GetErrors().Count > 0)
        {
            return new BadRequestObjectResult(requestValidator.GetErrors());
        }

        if (bulkCount > 0)
        {
            var randomList = randomIdentifier.GetRandomEnterpriseIdentifiers(bulkCount);
            var tokenList = await tokenHelper.GetTokenList(randomList, async randomOrgNo =>
                await tokenHelper.GetEnterpriseToken(req, env, scopes, org, randomOrgNo, supplierOrgNo, ttl, delegationSource, clientId));

            return new OkObjectResult(tokenList);
        }

        orgNo ??= randomIdentifier.GetRandomEnterpriseIdentifiers(1).First();
        var token = await tokenHelper.GetEnterpriseToken(req, env, scopes, org, orgNo, supplierOrgNo, ttl, delegationSource, clientId);

        return !string.IsNullOrEmpty(req.Query["dump"]) ? new OkObjectResult(tokenHelper.Dump(token)) : new OkObjectResult(token);
    }
}