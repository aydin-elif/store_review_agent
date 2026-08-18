using System.Security.Cryptography;
using ReviewAgent.Connectors.AppStore;

namespace ReviewAgent.Tests;

public class AppStoreJwtGeneratorTests
{
    [Fact]
    public void GenerateToken_ProducesValidJwtStructure()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string pem = ecdsa.ExportECPrivateKeyPem();

        AppStoreJwtGenerator generator = new("TESTKEYID", "TESTISSUERID", pem);
        string token = generator.GenerateToken();

        Assert.Equal(3, token.Split('.').Length);
    }
}
