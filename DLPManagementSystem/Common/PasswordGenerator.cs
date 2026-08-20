using System.Security.Cryptography;

namespace DLPManagementSystem.Common
{
    // Generates a random password to hand an admin-created account when no password was supplied -
    // replaces the old "hidden, unguessable hash" fallback (which left the account permanently
    // unable to sign in until a separate manual reset) with a real, usable password the caller can
    // read back once and give to the account holder. Always paired with User.MustChangePassword =
    // true so the account holder is forced to pick their own on first sign-in.
    public static class PasswordGenerator
    {
        private const string Uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lowers = "abcdefghijkmnpqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%^&*-_=+";
        private const int Length = 14;

        // Guarantees at least one character from each class (so the result can never accidentally
        // fail a complexity check downstream), then fills the rest from the combined pool and
        // shuffles - all via RandomNumberGenerator, not Random, since this is security-sensitive.
        public static string Generate()
        {
            var pools = new[] { Uppers, Lowers, Digits, Symbols };
            var combined = Uppers + Lowers + Digits + Symbols;

            var chars = new List<char>(Length);
            foreach (var pool in pools)
            {
                chars.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
            }

            while (chars.Count < Length)
            {
                chars.Add(combined[RandomNumberGenerator.GetInt32(combined.Length)]);
            }

            // Fisher-Yates, so the guaranteed-one-per-class characters aren't always in the first
            // four positions.
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }
    }
}
