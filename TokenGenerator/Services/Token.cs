using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class Token : IToken
    {
        private readonly Settings settings;
        private const string ValidScopeListRegex = @"^[a-z0-9:/_\-,\. ]+$";
        private readonly ICertificateService certificateHelper;

        public Token(IOptions<Settings> settings, ICertificateService certificateHelper)
        {
            this.settings = settings.Value;
            this.certificateHelper = certificateHelper;
        }

        public async Task<string> GetEnterpriseToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl, string delegationSource)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate(env);
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var payload = new JwtPayload
            {
                { "scope", string.Join(' ', scopes) },
                { "token_type", "Bearer" },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "client_id", Guid.NewGuid().ToString() },
                { "jti", RandomString(43) },
                { "consumer", GetOrgNoObject(orgNo) },
                { "urn:altinn:orgNumber", orgNo },
                { "urn:altinn:authenticatemethod", "maskinporten" },
                { "urn:altinn:authlevel", 3 },
                { "iss", GetIssuer(env) },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            if (!string.IsNullOrEmpty(org))
            {
                payload.Add("urn:altinn:org", org);
            }

            if (!string.IsNullOrEmpty(supplierOrgNo))
            {
                payload.Add("supplier", GetOrgNoObject(supplierOrgNo));
            }

            if (!string.IsNullOrEmpty(delegationSource))
            {
                payload.Add("delegation_source", delegationSource);
            }

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        public async Task<string> GetEnterpriseUserToken(string env, string[] scopes, string org, string orgNo, string supplierOrgNo, uint partyId, uint userId, string userName, uint ttl, string delegationSource)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate(env);
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var payload = new JwtPayload
            {
                { "scope", string.Join(' ', scopes) },
                { "token_type", "Bearer" },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "client_id", Guid.NewGuid().ToString() },
                { "consumer", GetOrgNoObject(orgNo) },
                { "jti", RandomString(43) },
                { "urn:altinn:userid", (int)userId },
                { "urn:altinn:username", userName },
                { "urn:altinn:partyid", (int)partyId },
                { "urn:altinn:orgNumber", orgNo },
                { "urn:altinn:authenticatemethod", "virksomhetsbruker" },
                { "urn:altinn:authlevel", 3 },
                { "iss", GetIssuer(env) },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            if (!string.IsNullOrEmpty(org))
            {
                payload.Add("urn:altinn:org", org);
            }

            if (!string.IsNullOrEmpty(supplierOrgNo))
            {
                payload.Add("supplier", GetOrgNoObject(supplierOrgNo));
            }

            if (!string.IsNullOrEmpty(delegationSource))
            {
                payload.Add("delegation_source", delegationSource);
            }

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        public async Task<string> GetSystemUserToken(string env, string[] scopes, string orgNo, string supplierOrgNo, string systemUserOrg, string systemUserId, string clientId, uint ttl)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate(env);
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var payload = new JwtPayload
            {
                { "iss", GetIssuer(env) },
                { "scope", string.Join(' ', scopes) },
                { "client_id", clientId },
                { "consumer", GetOrgNoObject(orgNo) },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "jti", RandomString(43) },
                { "authorization_details", GetAuthorizationDetailsForSystemUser(systemUserOrg, systemUserId) },
                { "urn:altinn:authenticatemethod", "systemuser" },
                { "urn:altinn:authlevel", 3 },
                { "token_type", "Bearer" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
                { "actual_iss", "altinn-test-tools" }
            };

            if (!string.IsNullOrEmpty(supplierOrgNo))
            {
                payload.Add("supplier", GetOrgNoObject(supplierOrgNo));
            }

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        public async Task<string> GetPersonalToken(string env, string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string clientAmr, uint ttl, string delegationSource)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate(env);
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var payload = new JwtPayload
            {
                { "urn:altinn:authenticatemethod", "NotDefined" },
                { "urn:altinn:authlevel", authLvl },
                { "client_amr", clientAmr },
                { "pid", pid },
                { "token_type", "Bearer" },
                { "client_id", Guid.NewGuid().ToString() },
                { "acr", GetAcrLevel(authLvl) },
                { "scope", string.Join(' ', scopes) },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "client_orgno", consumerOrgNo },
                { "consumer", GetOrgNoObject(consumerOrgNo) },
                { "iss", GetIssuer(env) },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            if (!string.IsNullOrEmpty(delegationSource))
            {
                payload.Add("delegation_source", delegationSource);
            }
            
            if (userId != 0) {
                payload.Add("nameid", (int)userId);
                payload.Add("urn:altinn:userid", (int)userId);
            }
            
            if (partyId != 0) {
                payload.Add("urn:altinn:partyid", (int)partyId);
            }

            if (userName != "") {
                payload.Add("urn:altinn:username", userName);
            }

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);

        }

        private string GetAcrLevel(string authLvl)
        {
            return authLvl switch
            {
                "3" => "idporten-loa-substantial",
                "4" => "idporten-loa-high",
                // NOTE! This is not currently a value that ID-porten produces
                // https://docs.digdir.no/docs/idporten/oidc/oidc_protocol_new_idporten#new-acr-values
                _ => "idporten-loa-low"
            };
        }

        public async Task<string> GetConsentToken(string env, string[] serviceCodes, IQueryCollection queryParameters,
            Guid authorizationCode, string offeredBy, string coveredBy, string handledBy, uint ttl)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetConsentTokenSigningCertificate(env);
            if (signingCertificate?.Thumbprint == null)
            {
                throw new ArgumentNullException($"GetApiTokenSigningCertificate({env}) returned null");
            }

            var securityKey = new X509SecurityKey(signingCertificate);
            var thumbprintHexBytes = new byte[signingCertificate.Thumbprint.Length / 2];
            for (var i = 0; i < signingCertificate.Thumbprint.Length; i += 2)
            {
                thumbprintHexBytes[i / 2] = Convert.ToByte(signingCertificate.Thumbprint.Substring(i, 2), 16);
            }

            var kidX5T = Base64UrlEncoder.Encode(thumbprintHexBytes);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
                { "x5t", kidX5T }
            };

            // Override default kid
            header.Remove("kid");
            header.Add("kid", kidX5T);

            var claims = new List<Claim>();
            foreach (var serviceCode in serviceCodes)
            {
                claims.Add(new Claim("Services", serviceCode));
                var metadataParameters = queryParameters.Where(x => x.Key.StartsWith(serviceCode))
                    .ToDictionary(p => p.Key, p => p.Value);
                foreach (var (key, value) in metadataParameters)
                {
                    claims.Add(new Claim("Services", key + "=" + value));
                }
            }

            claims.Add(new Claim("AuthorizationCode", authorizationCode.ToString()));
            claims.Add(new Claim("OfferedBy", offeredBy));
            claims.Add(new Claim("CoveredBy", coveredBy));
            if (handledBy != null)
            {
                claims.Add(new Claim("HandledBy", handledBy));
            }
            claims.Add(new Claim("DelegatedDate", (dateTimeOffset.ToUnixTimeSeconds() - 10).ToString(), ClaimValueTypes.Integer32));
            claims.Add(new Claim("ValidToDate", (dateTimeOffset.ToUnixTimeSeconds() + ttl).ToString(), ClaimValueTypes.Integer32));
            claims.Add(new Claim("iss", "altinn.no"));
            claims.Add(new Claim("actual_iss", "altinn-test-tools"));
            claims.Add(new Claim("exp", (dateTimeOffset.ToUnixTimeSeconds() + ttl).ToString(), ClaimValueTypes.Integer32));
            claims.Add(new Claim("nbf", dateTimeOffset.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer32));

            var securityToken = new JwtSecurityToken(header, new JwtPayload(claims));
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        /// <summary>
        /// Generates a type of AccessToken token and signing it with the same certificate as used by Authentication.
        /// </summary>
        /// <param name="env">The environment id.</param>
        /// <param name="appClaim">The name of the app.</param>
        /// <param name="ttl">Time to live.</param>
        public async Task<string> GetPlatformToken(string env, string appClaim, uint ttl)
        {
            string issuer = GetIssuer(env);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate(env);
            return CreateAccessToken(appClaim, ttl, issuer, signingCertificate);
        }

        /// <summary>
        /// Generates a "propper" AccessToken token and signing it with the same platform access token certificate.
        /// </summary>
        /// <remarks>The issuer is hard coded to <c>platform</c>. The value is used by AccessTokenHandler to identify
        /// correct public key when validating the token signature.</remarks>
        /// <param name="env">The environment id.</param>
        /// <param name="appClaim">The name of the platform application.</param>
        /// <param name="ttl">Time to live.</param>
        /// <param name="issuer">The issuer of the token. Default is <c>platform</c>.</param>
        public async Task<string> GetPlatformAccessToken(string env, string appClaim, uint ttl, string issuer)
        {
            if (issuer == settings.PlatformAccessTokenIssuerName)
            {
                var signingCertificate = await certificateHelper.GetPlatformAccessTokenSigningCertificate(env, settings.PlatformAccessTokenIssuerName);
                return CreateAccessToken(appClaim, ttl, settings.PlatformAccessTokenIssuerName, signingCertificate);
            }
            else if (issuer == settings.TtdAccessTokenIssuerName)
            {
                var signingCertificate = await certificateHelper.GetPlatformAccessTokenSigningCertificate(env, settings.TtdAccessTokenIssuerName);
                return CreateAccessToken(appClaim, ttl, settings.TtdAccessTokenIssuerName, signingCertificate);
            }
            else
            {
                throw new ArgumentException("Invalid issuer");
            }
        }

        public bool TryParseScopes(string input, out string[] scopes)
        {
            scopes = null;
            if (string.IsNullOrEmpty(input) || input.Length > 500 || !Regex.IsMatch(input, ValidScopeListRegex))
            {
                return false;
            }

            scopes = input.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        public bool IsValidIdentifier(string identifier)
        {
            return !string.IsNullOrEmpty(identifier) && identifier.Length <= 50 && Regex.IsMatch(identifier, "^[a-z0-9_-]+$");
        }

        public bool IsValidDottedIdentifier(string identifier)
        {
            return !string.IsNullOrEmpty(identifier) && identifier.Length <= 50 && Regex.IsMatch(identifier, @"^(?:[a-z0-9_]\.?)+[a-z0-9_]$");
        }

        public bool IsValidOrgNo(string orgNo)
        {
            return !string.IsNullOrEmpty(orgNo) && Regex.IsMatch(orgNo, "^[0-9]{9}$");
        }

        public bool IsValidPid(string pid)
        {
            return !string.IsNullOrEmpty(pid) && Regex.IsMatch(pid, "^[0-9]{11}$");
        }

        public bool IsValidPidOrOrgNo(string pidOrOrgNo)
        {
            return IsValidOrgNo(pidOrOrgNo) || IsValidPid(pidOrOrgNo);
        }

        public bool IsValidAuthLvl(string authLvl)
        {
            return authLvl == "3" || authLvl == "4";
        }

        public bool IsValidEnvironment(string env)
        {
            return settings.EnvironmentsApiTokenDict.ContainsKey(env);
        }

        public bool IsValidUri(string uriString)
        {
            return Uri.TryCreate(uriString, UriKind.Absolute, out _);
        }

        public bool IsValidServiceCodeList(string serviceCodes, out string[] serviceCodeList)
        {
            var tmp = new List<string>();
            foreach (var serviceCode in serviceCodes.Split('.'))
            {
                if (!Regex.IsMatch(serviceCode, @"^\w+_\w+$"))
                {
                    serviceCodeList = new string[] { };
                    return false;
                }

                tmp.Add(serviceCode);
            }

            serviceCodeList = tmp.ToArray();

            return true;
        }

        public string Dump(string token)
        {
            string[] base64Parts = token.Split('.').Take(2).ToArray();
            string[] jsonparts = base64Parts.Select(x =>
                Encoding.ASCII.GetString(
                    Convert.FromBase64String(x + new string('=', (4 - x.Length % 4) % 4))
                )
            ).ToArray();

            return
                JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonparts[0]), Formatting.Indented) + "\n.\n" +
                JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonparts[1]), Formatting.Indented);
        }

        private readonly Random random = new Random();

        private string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static Dictionary<string, string> GetOrgNoObject(string orgNo)
        {
            return new Dictionary<string, string>() { { "authority", "iso6523-actorid-upis" }, { "ID", "0192:" + orgNo } };
        }

        private static List<Dictionary<string, object>> GetAuthorizationDetailsForSystemUser(string systemUserOrg, string systemUserId)
        {
            var detailsDict = new Dictionary<string, object>
            {
                { "type", "urn:altinn:systemuser" },
                { "systemuser_id", new[] { systemUserId } },
                { "systemuser_org", GetOrgNoObject(systemUserOrg)},
                { "system_id", Guid.NewGuid().ToString() }
            };

            return new List<Dictionary<string, object>> { detailsDict };
        }

        private static string GetIssuer(string env)
        {
            string tld = (env.ToLowerInvariant().StartsWith("at") || env.ToLowerInvariant().StartsWith("yt01")) ? "cloud" : "no";
            return $"https://platform.{env}.altinn.{tld}/authentication/api/v1/openid/";
        }

        private static string CreateAccessToken(string appClaim, uint ttl, string issuer, X509Certificate2 signingCertificate)
        {
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var payload = new JwtPayload
            {
                { "urn:altinn:app", appClaim },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "iss", issuer },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }
    }
}
