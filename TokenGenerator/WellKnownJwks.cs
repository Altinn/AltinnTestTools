using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using TokenGenerator.Services.Interfaces;

public class WellKnownJwks(IIssuer issuer)
{
    private readonly JsonSerializerSettings jsonSettings = new() { NullValueHandling = NullValueHandling.Ignore };

    [Function("OpenIdConfiguration")]
    public Task<IActionResult> GetOpenIdConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/oauth-authorization-server")] HttpRequest req)
    {
        var baseUrl = $"{req.Scheme}://{req.Host}/api";
        var metadata = new
        {
            issuer = $"{baseUrl}/.well-known/oauth-authorization-server",
            jwks_uri = $"{baseUrl}/.well-known/jwks.json"
        };

        var json = JsonConvert.SerializeObject(metadata, jsonSettings);
        return Task.FromResult<IActionResult>(new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        });
    }

    [Function("JwksJson")]
    public Task<IActionResult> GetJwks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/jwks.json")] HttpRequest _)
    {
        var jwks = new
        {
            keys = issuer.GetPublicKeys().Select(k => new
            {
                k.Kid,
                k.Kty,
                k.Alg,
                k.Use,
                k.Crv,
                k.X,
                k.Y
            })
        };


        var json = JsonConvert.SerializeObject(jwks, jsonSettings);
        return Task.FromResult<IActionResult>(new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        });
    }
}
