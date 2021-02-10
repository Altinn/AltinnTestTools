using System;
using System.Collections.Generic;
using System.Text;

namespace TokenGenerator
{
    public class Settings
    {
        public string KeyVaultName { get; set; }
        public string ApiTokenSigningCertName { get; set; }
        public string ConsentTokenSigningCertName { get; set; }
    }
}
