using System;
using System.Security.Cryptography;
using System.Text;

namespace EnterprisePOS.Data
{
    /// <summary>
    /// Matches the scheme already stored in dbo.Users:
    /// PasswordHash = SHA-256( Salt + Password ) as upper-case hex, Salt = 32 hex characters.
    /// </summary>
    public static class PasswordHasher
    {
        public static string NewSalt()
        {
            byte[] bytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return ToHex(bytes);
        }

        public static string Hash(string salt, string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(Encoding.ASCII.GetBytes(salt + password)));
            }
        }

        public static bool Verify(string salt, string password, string storedHash)
        {
            if (salt == null || password == null || storedHash == null) return false;
            string actual = Hash(salt.Trim(), password);
            return FixedTimeEquals(actual, storedHash.Trim().ToUpperInvariant());
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
