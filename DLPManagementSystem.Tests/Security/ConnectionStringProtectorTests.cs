using DLPManagementSystem.Common;
using Xunit;

namespace DLPManagementSystem.Tests.Security;

public class ConnectionStringProtectorTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_ReturnsOriginalString()
    {
        var original = "Server=161.97.90.171,8797;Database=DLPSystem;User Id=DLPSystem;Password=DLP#Sys_9vR!72qL@4xN_2026;TrustServerCertificate=True;Connect Timeout=60;";

        var encrypted = ConnectionStringProtector.Encrypt(original);
        Assert.StartsWith("Encrypted:", encrypted);

        var decrypted = ConnectionStringProtector.Decrypt(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_PowerShellGeneratedCipher_MatchesOriginal()
    {
        var original = "Server=161.97.90.171,8797;Database=DLPSystem;User Id=DLPSystem;Password=DLP#Sys_9vR!72qL@4xN_2026;TrustServerCertificate=True;Connect Timeout=60;";
        var encrypted = "Encrypted:TBCaFPWXo3/9odhyX2iWLD8cjdtZ8J6Z0z1MybCR4LXB3rgyDrP0/dIe9MDWo4JJGkZAB8V620ZycLAvfE2KIwtPyvAEO+4C3o5H8xhSDjm59BOY/305uy6ysYFzEf2BLlPN4TSDVOkp7srDNxtDZV5NZAT0OaaMhSm0+vXndM6qZ160mssqxzRlMTahjs7lf3B0WlpxvMf8idLsSUmEvMKAJy+vTu35lQg2XVj72UE=";

        var decrypted = ConnectionStringProtector.Decrypt(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Decrypt_UnencryptedPlainText_ReturnsSameString()
    {
        var plain = "Server=localhost;Database=TestDb;Trusted_Connection=True;";

        var result = ConnectionStringProtector.Decrypt(plain);
        Assert.Equal(plain, result);
    }

    [Fact]
    public void Decrypt_NullOrWhitespace_ReturnsSameOrEmpty()
    {
        Assert.Equal(string.Empty, ConnectionStringProtector.Decrypt(string.Empty));
        Assert.Equal(string.Empty, ConnectionStringProtector.Decrypt(null));
    }

    [Fact]
    public void EncryptAndDecrypt_WithCustomKey_RoundTripsSuccessfully()
    {
        var original = "Server=127.0.0.1;Database=Custom;User Id=user;Password=pass;";
        var customKey = "MyCustomSecretKeyThatIsDifferent!";

        var encrypted = ConnectionStringProtector.Encrypt(original, customKey);
        var decrypted = ConnectionStringProtector.Decrypt(encrypted, customKey);

        Assert.Equal(original, decrypted);
    }
}
