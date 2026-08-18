using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ReviewAgent.Connectors.AppStore;

public class AppStoreJwtGenerator
{
    private readonly string _keyId;
    private readonly string _issuerId;
    private readonly string _privateKeyPem;

    public AppStoreJwtGenerator(string keyId, string issuerId, string privateKeyPem)
    {
        _keyId = keyId;
        _issuerId = issuerId;
        _privateKeyPem = privateKeyPem;
    }

    public string GenerateToken()
    {
        using ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_privateKeyPem);

        ECDsaSecurityKey securityKey = new(ecdsa) { KeyId = _keyId };
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.EcdsaSha256);

        JwtHeader header = new(credentials);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        JwtPayload payload = new()
        {
            { "iss", _issuerId },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", now.AddMinutes(19).ToUnixTimeSeconds() },
            { "aud", "appstoreconnect-v1" }
        };

        JwtSecurityToken token = new(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
