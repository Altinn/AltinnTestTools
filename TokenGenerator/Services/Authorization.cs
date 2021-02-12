using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public class Authorization : IAuthorization
    {
        private readonly HttpContext ctx;

        public Authorization(IHttpContextAccessor contextAccessor)
        {
            ctx = contextAccessor.HttpContext;
        }

        public async Task<ActionResult> Authorize()
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

            return await handler.IsAuthorized(parts[1]);
        }
    }

    internal class BasicAuthenticationRequestResult : UnauthorizedResult 
    {
        public BasicAuthenticationRequestResult() : base()
        {
        }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            context.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"AltinnTestTools - TokenGenerator\"");
        }
    }
}
