using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Identity;

namespace DLPManagementSystem.Service.Service
{
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordHasher<User> _hasher;

        public PasswordService(IPasswordHasher<User> hasher)
        {
            _hasher = hasher;
        }

        public string HashPassword(User user, string password) => _hasher.HashPassword(user, password);

        public bool VerifyAndUpgrade(User user, string suppliedPassword)
        {
            if (IsLegacySha256Hash(user.PasswordHash))
            {
                if (user.PasswordHash != SecurityHashHelper.Sha256(suppliedPassword))
                {
                    return false;
                }

                user.PasswordHash = _hasher.HashPassword(user, suppliedPassword);
                return true;
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, suppliedPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return false;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, suppliedPassword);
            }

            return true;
        }

        // Every value SecurityHashHelper.Sha256 ever produced is exactly 64 hex characters (Convert.ToHexString
        // of a 32-byte SHA-256 digest) — a shape PasswordHasher's own output never takes, so this is a safe,
        // deterministic way to tell old accounts apart from already-migrated ones.
        private static bool IsLegacySha256Hash(string hash) =>
            hash.Length == 64 && hash.All(Uri.IsHexDigit);
    }
}
