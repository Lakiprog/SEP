namespace Telecom.DTO
{
    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public string UserType { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}