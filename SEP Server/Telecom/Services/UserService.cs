using Microsoft.EntityFrameworkCore;
using Telecom.Data;
using Telecom.Models;
using Telecom.Interfaces;
using Telecom.Enums;
using Telecom.DTO;

namespace Telecom.Services
{
    public class UserService : IUserService
    {
        private readonly TelecomDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public UserService(TelecomDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<User> GetUserById(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            return user;
        }

        public async Task<LoginResponse> Login(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                throw new Exception("Invalid username or password");
            }

            var token = _jwtService.GenerateToken(user);
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expirationInMinutes = int.Parse(jwtSettings["ExpirationInMinutes"]);

            return new LoginResponse
            {
                Token = token,
                UserType = user.UserType.ToString(),
                Username = user.Username,
                Email = user.Email,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationInMinutes)
            };
        }

        public async Task<User> RegisterUser(User user)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email);

            if (existingUser != null)
            {
                throw new Exception("Username or email already exists");
            }

            user.UserType = UserType.Individual;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
