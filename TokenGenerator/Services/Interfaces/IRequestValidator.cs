using System;
using System.Collections.Generic;

namespace TokenGenerator.Services
{
    public interface IRequestValidator
    {
        void ValidateQueryParam(string field, bool isRequired, Func<string, bool> validator, out string output, string defaultValue = null);
        void ValidateQueryParam<OutT>(string field, bool isRequired, RequestValidator.ValidatorParser<string, OutT, bool> validator, out OutT output, OutT defaultValue = default);
        Dictionary<string, string> GetErrors();
    }
}