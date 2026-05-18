using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class AuthorizationBearer(IOptions<Settings> settings, ILogger<AuthorizationBearer> logger) : IAuthorizationBearer
{
    private static readonly HttpClient ConfigurationHttpClient = new() { Timeout = TimeSpan.FromMilliseconds(10000) };
    private readonly Settings settings = settings.Value;
    private readonly Lock cmLockMaskinporten = new();

    private ConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager
    {
        get
        {
            if (field != null) return field;
            lock (cmLockMaskinporten)
            {
                field ??= new ConfigurationManager<OpenIdConnectConfiguration>(
                    settings.TokenAuthorizationWellKnownEndpoint,
                    new OpenIdConnectConfigurationRetriever(),
                    ConfigurationHttpClient);
            }

            return field;
        }
    }

    public async Task<ActionResult> IsAuthorized(string authorizationString, string requiredScope, HttpContext httpContext)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(authorizationString);

            if (jwtToken.SignatureAlgorithm != SecurityAlgorithms.RsaSha256)
            {
                return new BadRequestObjectResult("Expected RsaSha256 signature");
            }

            var configuration = await ConfigurationManager.GetConfigurationAsync();
            var signingKeys = new List<SecurityKey>();
            signingKeys.AddRange(configuration.SigningKeys);

            var parameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKeys = signingKeys,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(1)
            };

            var principal = tokenHandler.ValidateToken(authorizationString, parameters, out var _);

            var scopeClaim = principal.Claims.FirstOrDefault(x => x.Type == "scope");
            if (scopeClaim == null)
            {
                return new UnauthorizedObjectResult("Missing required scope: " + requiredScope) { StatusCode = 403 };
            }

            var scopes = scopeClaim.Value.Split(' ');

            // Do a substring match, eg. having "altinn:testtools/tokengenerator" should satisfy a requirement for "altinn:testtools/tokengenerator/personal"
            if (scopes.Any(requiredScope.Contains))
            {
                var consumerClaim = principal.Claims.FirstOrDefault(x => x.Type == "consumer");

                httpContext.Items["AuthenticatedParty"] = consumerClaim == null ? "unknown" : GetOrganizationNumberFromClaimValue(consumerClaim.Value);
                return null;
            }

            return new UnauthorizedObjectResult("Missing required scope: " + requiredScope) { StatusCode = 403 };

        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Bearer authorization failed");
            return new UnauthorizedObjectResult("Invalid bearer token") { StatusCode = 403 };
        }
    }

    private static string GetOrganizationNumberFromClaimValue(string rawClaimValue)
    {
        ConsumerClaim consumerClaim;
        try
        {
            consumerClaim = JsonConvert.DeserializeObject<ConsumerClaim>(rawClaimValue);
        }
        catch (JsonReaderException)
        {
            throw new ArgumentException("Invalid consumer claim: invalid JSON");
        }

        if (consumerClaim.Authority != "iso6523-actorid-upis")
        {
            throw new ArgumentException("Invalid consumer claim: unexpected authority");
        }

        var identityParts = consumerClaim.Id.Split(':');
        if (identityParts[0] != "0192")
        {
            throw new ArgumentException("Invalid consumer claim: unexpected ISO6523 identifier");
        }

        return identityParts[1];
    }
}

internal sealed class ConsumerClaim
{
    /// <summary>
    /// Gets or sets the format of the identifier. Must always be "iso6523-actorid-upis"
    /// </summary>
    [DataMember(Name = "authority")]
    public string Authority { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the consumer. Must have ISO6523 prefix, which should be "0192:" for norwegian organization numbers
    /// </summary>
    [DataMember(Name = "ID")]
    public string Id { get; set; }
}
