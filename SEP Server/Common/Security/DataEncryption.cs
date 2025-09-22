using System.Security.Cryptography;
using System.Text;

namespace Common.Security
{
    public static class DataEncryption
    {
        private static readonly byte[] Key = Convert.FromBase64String(
            Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ??
            "YourBase64EncryptionKeyHere1234567890ABCDEF="); // Fallback key - should be set via environment variable

        /// <summary>
        /// Encrypts sensitive data using AES-256-CBC
        /// </summary>
        /// <param name="plainText">The data to encrypt</param>
        /// <returns>Encrypted data as base64 string</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Write IV first
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var writer = new StreamWriter(cs))
                        {
                            writer.Write(plainText);
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        /// <summary>
        /// Decrypts data encrypted with Encrypt method
        /// </summary>
        /// <param name="encryptedData">The encrypted data as base64 string</param>
        /// <returns>Decrypted plain text</returns>
        public static string Decrypt(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return encryptedData;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedData);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;

                    // Extract IV
                    byte[] iv = new byte[aes.BlockSize / 8];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    // Extract encrypted data
                    byte[] cipher = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipher))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        /// <summary>
        /// Masks sensitive data for display purposes (shows only last 4 characters)
        /// </summary>
        /// <param name="sensitiveData">The sensitive data to mask</param>
        /// <param name="visibleCount">Number of characters to show at the end</param>
        /// <returns>Masked string</returns>
        public static string MaskSensitiveData(string sensitiveData, int visibleCount = 4)
        {
            if (string.IsNullOrEmpty(sensitiveData) || sensitiveData.Length <= visibleCount)
                return new string('*', sensitiveData?.Length ?? 0);

            string visiblePart = sensitiveData.Substring(sensitiveData.Length - visibleCount);
            string maskedPart = new string('*', sensitiveData.Length - visibleCount);

            return maskedPart + visiblePart;
        }

        /// <summary>
        /// Generates a new encryption key for environment variable
        /// </summary>
        /// <returns>Base64 encoded encryption key</returns>
        public static string GenerateNewKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }
    }
}