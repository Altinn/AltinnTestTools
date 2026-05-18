using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class Authorization(ILogger<Authorization> logger) : IAuthorization
{
    public async Task<ActionResult> Authorize(string requiredScope, HttpRequest req)
    {
        var ctx = req.HttpContext;
        if (!ctx.Request.Headers.TryGetValue("Authorization", out var value))
        {
            return new BasicAuthenticationRequestResult();
        }

        var parts = value.ToString().Split(' ', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            return new BasicAuthenticationRequestResult();
        }

        IAuthorizationMethod handler = parts[0].ToLower() switch
        {
            "basic" => ctx.RequestServices.GetService<IAuthorizationBasic>(),
            "bearer" => ctx.RequestServices.GetService<IAuthorizationBearer>(),
            _ => null
        };

        if (handler == null)
        {
            return new BasicAuthenticationRequestResult();
        }

        var result = await handler.IsAuthorized(parts[1], requiredScope, ctx);
        if (result == null)
        {
            // Successfully authenticated, log who did this
            logger.LogInformation("Authenticated call by '{party}' for required scope '{requiredScope}'",
                ctx.Items["AuthenticatedParty"],
                requiredScope);
        }

        return result;
    }
}

internal class BasicAuthenticationRequestResult : UnauthorizedResult
{
    public override void ExecuteResult(ActionContext context)
    {
        base.ExecuteResult(context);
        context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"AltinnTestTools - TokenGenerator\"";
    }
}
