using System;
using System.Collections.Generic;
using System.Linq;

namespace TokenGenerator
{
    public class Settings
    {
        public string ApiTokenSigningCertNames { get; set; }
        public Dictionary<string, string> ApiTokenSigningCertNamesDict => GetKeyValuePairs(ApiTokenSigningCertNames);
        public string ConsentTokenSigningCertNames { get; set; }
        public Dictionary<string, string> ConsentTokenSigningCertNamesDict => GetKeyValuePairs(ConsentTokenSigningCertNames);
        public string BasicAuthorizationUsers { get; set; }
        public Dictionary<string, string> BasicAuthorizationUsersDict => GetKeyValuePairs(BasicAuthorizationUsers);
        public string AuthorizedScope { get; set; }
        public string TokenAuthorizationWellKnownEndpoint { get; set; }
        public string EnvironmentsApiToken { get; set; }
        public string EnvironmentsConsentToken { get; set; }
        public Dictionary<string, string> EnvironmentsApiTokenDict => GetKeyValuePairs(EnvironmentsApiToken);
        public Dictionary<string, string> EnvironmentsConsentTokenDict => GetKeyValuePairs(EnvironmentsConsentToken);

        private Dictionary<string, string> GetKeyValuePairs(string stringfieldToSpilt, char fieldSeparator = ';', char keyValueSeparator = ':')
        {
            Dictionary<string, string> keyValuePairs;
            try {
                keyValuePairs = stringfieldToSpilt.Split(fieldSeparator).Select(x => x.Split(keyValueSeparator)).ToDictionary(y => y[0], y => y[1]);
            }
            catch (Exception) 
            { 
                keyValuePairs = new Dictionary<string, string>();
            }

            return keyValuePairs;
        }
    }
}
