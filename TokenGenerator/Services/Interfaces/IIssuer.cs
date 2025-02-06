using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace TokenGenerator.Services.Interfaces;

public interface IIssuer
{
    SecurityKey GetSigningKey();
    string GetSigningKeyId();
    IEnumerable<JsonWebKey> GetPublicKeys();
}