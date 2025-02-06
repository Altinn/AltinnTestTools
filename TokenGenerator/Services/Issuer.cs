using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services;

public class Issuer : IIssuer
{
    private readonly List<(string KeyId, ECDsaSecurityKey PrivateKey, JsonWebKey PublicKey)> activeKeys;
    
    public Issuer(IOptions<Settings> settings)
    {
        var keys = new List<(string, ECDsaSecurityKey, JsonWebKey)>();
        
        foreach (var (keyId, base64PrivateKey) in settings.Value.IssuerSigningKeysDict)
        {
            try
            {
                var ecDsa = LoadPrivateKeyFromBase64(base64PrivateKey);
                var privateKey = new ECDsaSecurityKey(ecDsa) { KeyId = keyId };
                var publicKey = CreateJsonWebKey(ecDsa, keyId);
                keys.Add((keyId, privateKey, publicKey));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading key {keyId}: {ex.Message}");
            }
        }

        if (!keys.Any())
            throw new InvalidOperationException("No valid signing keys were loaded.");

        activeKeys = keys;
    }

    public SecurityKey GetSigningKey() => activeKeys.First().PrivateKey;

    public string GetSigningKeyId() => activeKeys.First().KeyId;

    public IEnumerable<JsonWebKey> GetPublicKeys() => activeKeys.Select(k => k.PublicKey);

    private static ECDsa LoadPrivateKeyFromBase64(string base64)
    {
        var keyBytes = Convert.FromBase64String(base64);
        var ecDsa = ECDsa.Create();
        ecDsa.ImportPkcs8PrivateKey(keyBytes, out _);
        return ecDsa;
    }

    private static JsonWebKey CreateJsonWebKey(ECDsa ecDsa, string keyId)
    {
        var parameters = ecDsa.ExportParameters(false);
        return new JsonWebKey
        {
            Kid = keyId,
            Kty = "EC",
            Use = "sig",
            Crv = "P-256",
            X = Base64UrlEncode(parameters.Q.X),
            Y = Base64UrlEncode(parameters.Q.Y),
            Alg = SecurityAlgorithms.EcdsaSha256
        };
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}