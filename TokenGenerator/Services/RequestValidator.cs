using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace TokenGenerator.Services
{
    public class RequestValidator : IRequestValidator
    {
        private readonly HttpRequest req;

        private const string MANDATORY_IS_MISSING = "Parameter must be supplied";
        private const string FAILED_VALIDATION = "Parameter was supplied with a invalid value";
        private Dictionary<string, string> errors = new Dictionary<string, string>();

        public delegate V ValidatorParser<T, U, V>(T input, out U output);

        public RequestValidator(IHttpContextAccessor contextAccessor)
        {
            this.req = contextAccessor.HttpContext.Request;
        }

        public void ValidateQueryParam<OutT>(string field, bool isRequired, ValidatorParser<string, OutT, bool> validator, out OutT output, OutT defaultValue = default)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) errors.Add(field, MANDATORY_IS_MISSING);
                return;
            }

            if (!validator(req.Query[field], out output))
            {
                errors.Add(field, FAILED_VALIDATION);
            }
        }

        public void ValidateQueryParam(string field, bool isRequired, Func<string, bool> validator, out string output, string defaultValue = null)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) errors.Add(field, MANDATORY_IS_MISSING);
                return;
            }

            if (!validator(req.Query[field]))
            {
                errors.Add(field, FAILED_VALIDATION);
            }
            else
            {
                output = req.Query[field];
            }
        }

        public Dictionary<string, string> GetErrors()
        {
            return errors;
        }
    }
}
