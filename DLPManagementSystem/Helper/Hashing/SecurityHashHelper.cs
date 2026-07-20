using System.Security.Cryptography;
using System.Text;

namespace DLPManagementSystem.Helper.Hashing
{
    public static class SecurityHashHelper
    {
        public static string Sha256(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes);
        }

        public static string GenerateSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }
    }
}
