using System.Security.Cryptography;
using System.Text;

namespace DLPManagementSystem.Common;

/// <summary>
/// Provides transparent AES-256 encryption and decryption for connection strings.
/// Supports both raw plaintext and "Encrypted:..." prefixed strings.
/// </summary>
public static class ConnectionStringProtector
{
    public const string EncryptedPrefix = "Encrypted:";

    // Default 256-bit encryption key (32 bytes)
    private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("DLP#Sys_Key$2026_SecuredDbConn!!");

    /// <summary>
    /// Encrypts a plaintext connection string using AES-256 with a random IV.
    /// </summary>
    public static string Encrypt(string plainText, string? customKey = null)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        var keyBytes = GetKeyBytes(customKey);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipherBytes
        var combined = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, iv.Length, cipherBytes.Length);

        return $"{EncryptedPrefix}{Convert.ToBase64String(combined)}";
    }

    /// <summary>
    /// Decrypts a connection string if it starts with "Encrypted:".
    /// If it does not start with "Encrypted:", returns it as-is.
    /// </summary>
    public static string Decrypt(string? cipherText, string? customKey = null)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return cipherText ?? string.Empty;

        if (!cipherText.StartsWith(EncryptedPrefix, StringComparison.OrdinalIgnoreCase))
            return cipherText;

        var base64Data = cipherText.Substring(EncryptedPrefix.Length).Trim();
        var combined = Convert.FromBase64String(base64Data);

        if (combined.Length <= 16)
            throw new FormatException("Invalid encrypted connection string payload.");

        var keyBytes = GetKeyBytes(customKey);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[16];
        var cipherBytes = new byte[combined.Length - 16];

        Buffer.BlockCopy(combined, 0, iv, 0, 16);
        Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static byte[] GetKeyBytes(string? customKey)
    {
        if (string.IsNullOrWhiteSpace(customKey))
            return DefaultKey;

        var bytes = Encoding.UTF8.GetBytes(customKey);
        if (bytes.Length == 32)
            return bytes;

        // If not exactly 32 bytes, hash to 32 bytes via SHA-256
        return SHA256.HashData(bytes);
    }
}

