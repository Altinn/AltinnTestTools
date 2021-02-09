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
    }
}
