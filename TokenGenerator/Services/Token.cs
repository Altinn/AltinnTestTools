using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TokenGenerator.Services
{
    public class Token : IToken
    {
        private readonly ICertificateService certificateHelper;

        public Token(ICertificateService certificateHelper)
        {
            this.certificateHelper = certificateHelper;
        }

        public async Task<string> GetEnterpriseToken(string[] scopes, string org, string orgNo, string supplierOrgNo, uint ttl)
        {

            /*
                {
                  "scope": "altinn:whatever",
                  "token_type": "Bearer",
                  "exp": 1612629871,
                  "iat": 1612628071,
                  "client_id": "806e1e80-e3a7-4a73-980e-f92ba1c2bf86",
                  "jti": "P3IqinzPNJaW8k7Y235vz0sqrWAXmASBlDGHtAqH-Ac",
                  "consumer": {
                    "authority": "iso6523-actorid-upis",
                    "ID": "0192:991825827"
                  },
                  "urn:altinn:org": "digdir",
                  "urn:altinn:orgNumber": 991825827,
                  "urn:altinn:authenticatemethod": "maskinporten",
                  "urn:altinn:authlevel": 3,
                  "iss": "https://platform.tt02.altinn.no/",
                  "nbf": 1612628071
                }
            */

            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate();
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
                { "urn:altinn:org", org },
                { "urn:altinn:orgNumber", orgNo },
                { "urn:altinn:authenticatemethod", "maskinporten" },
                { "urn:altinn:authlevel", 3 },
                { "iss", "https://platform.tt02.altinn.no/" },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            if (!string.IsNullOrEmpty(supplierOrgNo))
            {
                payload.Add("supplier", GetOrgNoObject(supplierOrgNo));
            }

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }

        public async Task<string> GetPersonalToken(string[] scopes, uint userId, uint partyId, string pid, string authLvl, string consumerOrgNo, string userName, string client_amr, uint ttl)
        {
            /*
            {
              "nameid": "61218",
              "urn:altinn:userid": "61218",
              "urn:altinn:username": "larsdreyer",
              "urn:altinn:partyid": 50042162,
              "urn:altinn:authenticatemethod": "NotDefined",
              "urn:altinn:authlevel": 3,
              "client_amr": "virksomhetssertifikat",
              "pid": "11115601999",
              "token_type": "Bearer",
              "client_id": "fbce2007-2efb-4d3c-a1fa-919d6d328135",
              "acr": "Level3",
              "scope": "altinn:reportees altinn:profiles.read altinn:instances.read altinn:instances.write",
              "exp": 1612825327,
              "iat": 1612823527,
              "client_orgno": "991825827",
              "consumer": {
                "authority": "iso6523-actorid-upis",
                "ID": "0192:991825827"
              },
              "iss": "https://platform.tt02.altinn.no/",
              "nbf": 1612823527
            }
            */

            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
            var signingCertificate = await certificateHelper.GetApiTokenSigningCertificate();
            var securityKey = new X509SecurityKey(signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
               { "x5c", signingCertificate.Thumbprint }
            };

            var payload = new JwtPayload
            {
                { "nameid", userId },
                { "urn:altinn:userid", userId },
                { "urn:altinn:username", userName },
                { "urn:altinn:partyid", partyId },
                { "urn:altinn:authenticatemethod", "NotDefined" },
                { "urn:altinn:authlevel", authLvl },
                { "client_amr", client_amr },
                { "pid", pid },
                { "token_type", "Bearer" },
                { "client_id", Guid.NewGuid().ToString() },
                { "acr", "Level" + authLvl },
                { "scope", string.Join(' ', scopes) },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + ttl },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "client_orgno", consumerOrgNo },
                { "consumer", GetOrgNoObject(consumerOrgNo) },
                { "iss", "https://platform.tt02.altinn.no/" },
                { "actual_iss", "altinn-test-tools" },
                { "nbf", dateTimeOffset.ToUnixTimeSeconds() },
            };

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);

        }

        public bool TryParseScopes(string input, out string[] scopes)
        {
            scopes = null;
            if (string.IsNullOrEmpty(input) || input.Length > 200 || !Regex.IsMatch(input, "^[a-z0-9: ,]+$"))
            {
                return false;
            }

            scopes = input.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        public bool IsValidIdentifier(string identifier)
        {
            return !string.IsNullOrEmpty(identifier) && identifier.Length <= 50 && Regex.IsMatch(identifier, "^[a-z]+$");
        }

        public bool IsValidOrgNo(string orgNo)
        {
            return !string.IsNullOrEmpty(orgNo) && Regex.IsMatch(orgNo, "^[0-9]{9}$");
        }

        public bool IsValidPid(string pid)
        {
            return !string.IsNullOrEmpty(pid) && Regex.IsMatch(pid, "^[0-9]{11}$");
        }

        public bool IsValidAuthLvl(string authlvl)
        {
            return authlvl == "3" || authlvl == "4";
        }

        public string Dump(string token)
        {
            string[] base64parts = token.Split('.').Take(2).ToArray();
            string[] jsonparts = base64parts.Select(x =>
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

        private Dictionary<string, string> GetOrgNoObject(string orgNo)
        {
            return new Dictionary<string, string>() { { "authority", "iso6523-actorid-upis" }, { "ID", "0192:" + orgNo } };
        }
    }
}
