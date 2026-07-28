using System;
using System.Security.Cryptography;

namespace TD_KT.Services
{
    /// <summary>
    /// PBKDF2 password hashing (hash + salt + iterations).
    /// - Compatible with .NET Framework 4.8 (Rfc2898DeriveBytes default = HMACSHA1).
    /// - Stores hash/salt as Base64 strings.
    /// </summary>
    public static class PasswordHasherService
    {
        public const string DefaultAlgorithm = "PBKDF2-SHA1";
        public const int DefaultIterations = 100_000;
        public const int SaltSizeBytes = 16;
        public const int HashSizeBytes = 32;

        public sealed class HashResult
        {
            public string HashBase64 { get; set; }
            public string SaltBase64 { get; set; }
            public string Algorithm { get; set; }
            public int Iterations { get; set; }
        }

        public static HashResult HashPassword(string password, int iterations = DefaultIterations)
        {
            if (password == null) password = string.Empty;
            if (iterations <= 0) iterations = DefaultIterations;

            var salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                hash = pbkdf2.GetBytes(HashSizeBytes);
            }

            return new HashResult
            {
                HashBase64 = Convert.ToBase64String(hash),
                SaltBase64 = Convert.ToBase64String(salt),
                Algorithm = DefaultAlgorithm,
                Iterations = iterations
            };
        }

        public static bool VerifyPassword(string password, string storedHashBase64, string storedSaltBase64, int iterations, string algorithm)
        {
            if (string.IsNullOrWhiteSpace(storedHashBase64) || string.IsNullOrWhiteSpace(storedSaltBase64))
                return false;

            if (password == null) password = string.Empty;
            if (iterations <= 0) iterations = DefaultIterations;

            // Chỉ hỗ trợ PBKDF2-SHA1 hiện tại
            if (!string.IsNullOrWhiteSpace(algorithm) &&
                !algorithm.Equals("PBKDF2-SHA1", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;

            try
            {
                salt = Convert.FromBase64String(storedSaltBase64);
                expectedHash = Convert.FromBase64String(storedHashBase64);
            }
            catch (FormatException)
            {
                // Hash/Salt không phải Base64 hợp lệ -> không verify được
                return false;
            }

            byte[] actualHash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                actualHash = pbkdf2.GetBytes(expectedHash.Length);
            }

            return FixedTimeEquals(expectedHash, actualHash);
        }


        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
    }
}
