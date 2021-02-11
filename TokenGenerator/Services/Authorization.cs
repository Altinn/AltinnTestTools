using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TokenGenerator.Services
{
    public class Authorization : IAuthorization
    {
        private readonly HttpContext ctx;

        public Authorization(IHttpContextAccessor contextAccessor)
        {
            ctx = contextAccessor.HttpContext;
        }

        public bool Authorize(out ActionResult result)
        {
            if (!ctx.Request.Headers.ContainsKey("Authorization"))
            {
                result = new BasicAuthenticationRequestResult();
                return false;
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
                result = new BasicAuthenticationRequestResult();
                return false;
            }

            if (!handler.IsAuthorized(parts[1], out ActionResult failedAuthorizationResult)) 
            {
                result = failedAuthorizationResult;
                return false;
            }

            result = null;
            return true;
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
