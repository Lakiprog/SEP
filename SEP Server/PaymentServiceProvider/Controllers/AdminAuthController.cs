using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/admin-auth")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private const string ADMIN_USERNAME = "admin";
        private const string ADMIN_PASSWORD = "admin123"; // In production, use proper password hashing

        /// <summary>
        /// Authenticate admin user
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginRequest request)
        {
            try
            {
                // Simple authentication (in production, use proper authentication)
                if (request.Username == ADMIN_USERNAME && request.Password == ADMIN_PASSWORD)
                {
                    // Generate admin session token
                    var token = GenerateAdminToken(request.Username);

                    return Ok(new AdminLoginResponse
                    {
                        Success = true,
                        Token = token,
                        User = new AdminUserInfo
                        {
                            Username = request.Username,
                            Role = "Administrator"
                        }
                    });
                }

                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Validate admin token
        /// </summary>
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                var isValid = ValidateAdminToken(request.Token);
                if (isValid)
                {
                    return Ok(new { valid = true, role = "Administrator" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Logout admin user
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // In a real implementation, you would invalidate the token
            return Ok(new { message = "Logged out successfully" });
        }

        #region Helper Methods

        private string GenerateAdminToken(string username)
        {
            var payload = $"admin:{username}:{DateTime.UtcNow.Ticks}";
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        private bool ValidateAdminToken(string token)
        {
            try
            {
                // Simple token validation (in production, use proper JWT validation)
                // For demo purposes, we'll just check if it's a valid base64 string
                if (string.IsNullOrEmpty(token))
                    return false;

                var decodedBytes = Convert.FromBase64String(token);
                var decodedString = Encoding.UTF8.GetString(decodedBytes);
                
                // Check if it contains admin identifier
                return decodedString.Contains("admin:");
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    #region Request/Response Models

    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public AdminUserInfo User { get; set; } = new();
    }

    public class AdminUserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    #endregion
}
