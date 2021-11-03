using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class AuthorizationBasic : IAuthorizationBasic
    {
        private readonly Settings settings;
        private readonly HttpContext httpContext;

        public AuthorizationBasic(IOptions<Settings> settings, IHttpContextAccessor contextAccessor)
        {
            this.settings = settings.Value;
            this.httpContext = contextAccessor.HttpContext;
        }

        public async Task<ActionResult> IsAuthorized(string authorizationString, string _)
        {
            if (!ParseUserNamePassword(authorizationString, out string userName, out string password))
            {
                return new BadRequestResult();
            }

            if (!IsUserAuthorized(userName, password)) {
                return new BasicAuthenticationRequestResult();
            }

            httpContext.Items["AuthenticatedParty"] = userName;

            return await Task.FromResult<ActionResult>(null);
        }

        private bool ParseUserNamePassword(string rawInput, out string userName, out string password)
        {
            userName = null;
            password = null;
            try
            {
                string[] parts = Encoding.UTF8.GetString(
                    // Add padding if missing
                    Convert.FromBase64String(rawInput + new string('=', (4 - rawInput.Length % 4) % 4))).Split(':', 2);
                userName = parts[0];
                password = parts[1];

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsUserAuthorized(string userName, string password)
        {
            return settings.BasicAuthorizationUsersDict.ContainsKey(userName) && string.Equals(settings.BasicAuthorizationUsersDict[userName], password);
        }
    }
}
