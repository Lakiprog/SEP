using System.Security.Cryptography;
using System.Text;

namespace Common.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 32; // 256 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 10000; // PBKDF2 iterations

        /// <summary>
        /// Hashes a password using PBKDF2 with salt
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>The hashed password with salt encoded as base64</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            // Combine salt and hash
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            // Convert to base64 for storage
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <param name="hashedPassword">The stored hash</param>
        /// <returns>True if the password matches the hash</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                // Convert from base64
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                // Extract salt
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Extract hash
                byte[] storedHash = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

                // Hash the provided password with the same salt
                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

                // Compare hashes
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}