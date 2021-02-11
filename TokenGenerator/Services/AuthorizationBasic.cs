using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace TokenGenerator.Services
{
    public class AuthorizationBasic : IAuthorizationBasic
    {
        private readonly Settings settings;

        public AuthorizationBasic(IOptions<Settings> settings)
        {
            this.settings = settings.Value;
        }

        public bool IsAuthorized(string authorizationString, out ActionResult authorizationFailureResult)
        {
            if (!ParseUserNamePassword(authorizationString, out string userName, out string password))
            {
                authorizationFailureResult = new BadRequestResult();
                return false;
            }

            if (!IsUserAuthorized(userName, password)) {
                authorizationFailureResult = new BasicAuthenticationRequestResult();
                return false;
            }

            authorizationFailureResult = null;
            return true;
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
