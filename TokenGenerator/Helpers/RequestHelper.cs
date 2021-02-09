using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace TokenGenerator.Helpers
{
    public class RequestHelper
    {
        public delegate V ValidatorParser<T,U,V>(T input, out U output);
        public Dictionary<string, string> Errors = new Dictionary<string, string>();

        private readonly HttpRequest req;
        private const string MANDATORY_IS_MISSING = "Parameter must be supplied";
        private const string FAILED_VALIDATION = "Parameter was supplied with a invalid value";

        public RequestHelper(HttpRequest req)
        {
            this.req = req;
        }

        public void ValidateQueryParam<OutT>(string field, bool isRequired, ValidatorParser<string, OutT, bool> validator, out OutT output, OutT defaultValue = default)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) Errors.Add(field, MANDATORY_IS_MISSING);
                return;
            }

            if (!validator(req.Query[field], out output))
            {
                Errors.Add(field, FAILED_VALIDATION);
            }
        }

        public void ValidateQueryParam(string field, bool isRequired, Func<string, bool> validator, out string output, string defaultValue = null)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) Errors.Add(field, MANDATORY_IS_MISSING);
                return;
            }

            if (!validator(req.Query[field]))
            {
                Errors.Add(field, FAILED_VALIDATION);
            }
            else
            {
                output = req.Query[field];
            }
        }

        public bool Authorize(out ActionResult result)
        {
            result = null;
            if (!req.Headers.ContainsKey("Authorization"))
            {
                result = new BasicAuthenticationRequestResult();
                return false;
            }

            string[] parts = req.Headers["Authorization"].ToString().Split(' ', 2);

            switch (parts[0].ToLower())
            {
                case "basic":
                    if (!ParseUserNamePassword(parts[1], out string userName, out string password))
                    {
                        result = new BadRequestResult();
                        return false;
                    }

                    if (!AuthorizeUser(userName, password))
                    {
                        result = new BasicAuthenticationRequestResult();
                        return false;
                    }

                    return true;

                case "bearer":
                    if (!ValidateOAuth2Token(parts[1]))
                    {
                        result = new StatusCodeResult(403);
                        return false;
                    }

                    return true;

                default:
                    result = new BasicAuthenticationRequestResult();
                    return false;
            }
        }

        private bool ParseUserNamePassword(string rawInput, out string userName, out string password)
        {
            userName = null;
            password = null;
            try
            {
                string[] parts = Encoding.UTF8.GetString(Convert.FromBase64String(rawInput + new string('=', (4 - rawInput.Length % 4) % 4))).Split(':', 2);
                userName = parts[0];
                password = parts[1];

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool AuthorizeUser(string userName, string password)
        {
            return userName == "bjorn";
        }

        private bool ValidateOAuth2Token(string token)
        {
            return false; // TODO!
        }
    }

    public class BasicAuthenticationRequestResult : UnauthorizedResult 
    {
        public BasicAuthenticationRequestResult() : base()
        {
        }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            context.HttpContext.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"AltinnTestTools\"");
        }
    }
}
