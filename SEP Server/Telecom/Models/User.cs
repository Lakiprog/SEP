using System.ComponentModel.DataAnnotations;
using Telecom.Enums;

namespace Telecom.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public UserType UserType { get; set; }
    }
}
