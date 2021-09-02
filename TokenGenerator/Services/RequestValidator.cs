using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class RequestValidator : IRequestValidator
    {
        private readonly HttpRequest req;

        private const string MandatoryIsMissing = "Parameter must be supplied";
        private const string FailedValidation = "Parameter was supplied with a invalid value";
        private readonly Dictionary<string, string> errors = new Dictionary<string, string>();

        public delegate T ValidatorParser<in TIn, TOut, out T>(TIn input, out TOut output);

        public RequestValidator(IHttpContextAccessor contextAccessor)
        {
            req = contextAccessor.HttpContext.Request;
        }

        public void ValidateQueryParam<TOut>(string field, bool isRequired, ValidatorParser<string, TOut, bool> validator, out TOut output, TOut defaultValue = default)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) errors.Add(field, MandatoryIsMissing);
                return;
            }

            if (!validator(req.Query[field], out output))
            {
                errors.Add(field, FailedValidation);
            }
        }

        public void ValidateQueryParam(string field, bool isRequired, Func<string, bool> validator, out string output, string defaultValue = null)
        {
            output = defaultValue;
            if (string.IsNullOrEmpty(req.Query[field]))
            {
                if (isRequired) errors.Add(field, MandatoryIsMissing);
                return;
            }

            if (!validator(req.Query[field]))
            {
                errors.Add(field, FailedValidation);
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
