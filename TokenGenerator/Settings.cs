using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TokenGenerator
{
    public class Settings
    {
        private Dictionary<string, string> _basicAuthorizationUsersDict = null;

        public string KeyVaultName { get; set; }
        public string ApiTokenSigningCertName { get; set; }
        public string ConsentTokenSigningCertName { get; set; }
        public string BasicAuthorizationUsers { get; set; }
        public Dictionary<string, string> BasicAuthorizationUsersDict { 
            get {
                if (_basicAuthorizationUsersDict == null)
                {
                    try {
                        _basicAuthorizationUsersDict = BasicAuthorizationUsers.Split(';').Select(x => x.Split(':')).ToDictionary(y => y[0], y => y[1]);
                    }
                    catch (Exception) {
                       _basicAuthorizationUsersDict = new Dictionary<string, string>();
                    }
                }

                return _basicAuthorizationUsersDict;
            }
        }
        public string AuthorizedScope { get; set; }
        public string TokenAuthorizationWellKnownEndpoint { get; set; }
    }
}
