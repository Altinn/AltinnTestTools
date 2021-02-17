using System;
using System.Collections.Generic;
using System.Text;

namespace TokenGeneratorCli
{
    public class Settings
    {
        #region Base settings
        public string BaseUrl { get; set; }
        public string AuthorzationUserName { get; set; }
        public string AuthorizationPassword { get; set; }
        #endregion

        #region Common settings
        public string Environment { get; set; }
        public string Scopes { get; set; }
        public string Ttl { get; set; }
        #endregion

        #region Enterprise token settings
        public string Org { get; set; }
        public string OrgNo { get; set; }
        public string SupplierOrgNo { get; set; }
        #endregion

        #region Personal token settings
        public string UserId { get; set; }
        public string PartyId { get; set; }
        public string Pid { get; set; }
        public string AuthLvl { get; set; }
        public string ConsumerOrgNo { get; set; }
        public string UserName { get; set; }
        public string ClientAmr { get; set; }
        #endregion
    }
}
