using System;
using System.Diagnostics;

namespace Common.Security
{
    /// <summary>
    /// Simple test class to validate encryption and password hashing functionality
    /// </summary>
    public static class SecurityTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Security Implementation Tests ===\n");

            TestPasswordHashing();
            TestDataEncryption();
            TestMasking();

            Console.WriteLine("=== All Security Tests Completed ===");
        }

        private static void TestPasswordHashing()
        {
            Console.WriteLine("1. Testing Password Hashing...");

            string password = "TestPassword123!";
            string hashedPassword = PasswordHasher.HashPassword(password);

            Console.WriteLine($"   Original: {password}");
            Console.WriteLine($"   Hashed: {hashedPassword.Substring(0, 20)}...");

            // Test verification
            bool isValid = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool isInvalid = PasswordHasher.VerifyPassword("WrongPassword", hashedPassword);

            Console.WriteLine($"   Correct password verification: {isValid}");
            Console.WriteLine($"   Wrong password verification: {isInvalid}");

            if (isValid && !isInvalid)
            {
                Console.WriteLine("   ✓ Password hashing test PASSED\n");
            }
            else
            {
                Console.WriteLine("   ✗ Password hashing test FAILED\n");
            }
        }

        private static void TestDataEncryption()
        {
            Console.WriteLine("2. Testing Data Encryption...");

            string[] testData = {
                "4111111111111111",  // Card number
                "12/25",             // Expiry date
                "123",               // CVC
                "api_key_12345",     // API key
                "webhook_secret_xyz" // Webhook secret
            };

            foreach (string data in testData)
            {
                try
                {
                    string encrypted = DataEncryption.Encrypt(data);
                    string decrypted = DataEncryption.Decrypt(encrypted);

                    Console.WriteLine($"   Original: {data}");
                    Console.WriteLine($"   Encrypted: {encrypted.Substring(0, Math.Min(20, encrypted.Length))}...");
                    Console.WriteLine($"   Decrypted: {decrypted}");

                    if (data == decrypted)
                    {
                        Console.WriteLine("   ✓ Encryption/Decryption SUCCESS");
                    }
                    else
                    {
                        Console.WriteLine("   ✗ Encryption/Decryption FAILED");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ✗ Encryption test failed: {ex.Message}");
                }
                Console.WriteLine();
            }
        }

        private static void TestMasking()
        {
            Console.WriteLine("3. Testing Data Masking...");

            string[] testCards = {
                "4111111111111111",
                "5555555555554444",
                "378282246310005"
            };

            foreach (string cardNumber in testCards)
            {
                string masked = DataEncryption.MaskSensitiveData(cardNumber, 4);
                Console.WriteLine($"   Card: {cardNumber} -> Masked: {masked}");
            }

            Console.WriteLine("   ✓ Data masking test COMPLETED\n");
        }

        /// <summary>
        /// Generates a new encryption key for environment variable setup
        /// </summary>
        public static void GenerateNewEncryptionKey()
        {
            string newKey = DataEncryption.GenerateNewKey();
            Console.WriteLine("=== New Encryption Key Generated ===");
            Console.WriteLine($"ENCRYPTION_KEY={newKey}");
            Console.WriteLine("Add this to your environment variables or configuration.");
        }
    }
}