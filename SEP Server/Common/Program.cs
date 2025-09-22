using Common.Security;

namespace Common
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SEP Security Implementation Test");
            Console.WriteLine("================================\n");

            // Run security tests
            SecurityTest.RunTests();

            Console.WriteLine("\n--- Encryption Key Generation ---");
            SecurityTest.GenerateNewEncryptionKey();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}