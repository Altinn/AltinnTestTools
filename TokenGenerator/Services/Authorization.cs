using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class Authorization : IAuthorization
    {
        private readonly ILogger<Authorization> logger;
        private readonly HttpContext ctx;

        public Authorization(IHttpContextAccessor contextAccessor, ILogger<Authorization> logger)
        {
            this.logger = logger;
            ctx = contextAccessor.HttpContext;
        }

        public async Task<ActionResult> Authorize(string requiredScope)
        {
            if (!ctx.Request.Headers.ContainsKey("Authorization"))
            {
                return new BasicAuthenticationRequestResult();
            }

            string[] parts = ctx.Request.Headers["Authorization"].ToString().Split(' ', 2);
            IAuthorizationMethod handler = null;
            switch (parts[0].ToLower())
            {
                case "basic":
                    handler = ctx.RequestServices.GetService<IAuthorizationBasic>();
                    break;
                case "bearer":
                    handler = ctx.RequestServices.GetService<IAuthorizationBearer>();
                    break;
            }

            if (handler == null)
            {
                return new BasicAuthenticationRequestResult();
            }

            ActionResult result = await handler.IsAuthorized(parts[1], requiredScope);
            if (result == null)
            {
                // Successfully authenticated, log who did this
                logger.LogInformation("Authenticated call by '{party}' to '{endpoint}' with parameters '{query}'", ctx.Items["AuthenticatedParty"], ctx.Request.Path.ToString(), ctx.Request.QueryString.ToString());
            }

            return result;
        }
    }

    internal class BasicAuthenticationRequestResult : UnauthorizedResult 
    {
        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            context.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"AltinnTestTools - TokenGenerator\"");
        }
    }
}
