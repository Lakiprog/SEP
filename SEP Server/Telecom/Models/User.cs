using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Telecom.Enums;
using Common.Security;

namespace Telecom.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        private string _password = null!;

        /// <summary>
        /// Hashed password - automatically hashed when set
        /// </summary>
        public string Password
        {
            get => _password;
            set => _password = string.IsNullOrEmpty(value) ? value : PasswordHasher.HashPassword(value);
        }

        /// <summary>
        /// Verifies if the provided password matches the stored hash
        /// </summary>
        /// <param name="password">Password to verify</param>
        /// <returns>True if password matches</returns>
        public bool VerifyPassword(string password)
        {
            return PasswordHasher.VerifyPassword(password, _password);
        }

        public UserType UserType { get; set; }
    }
}
