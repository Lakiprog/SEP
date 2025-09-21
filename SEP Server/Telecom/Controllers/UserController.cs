using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Telecom.Interfaces;
using Telecom.Models;
using Telecom.DTO;

namespace Telecom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("registerUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterUser([FromBody] User user)
        {
            try
            {
                var registerUser = await _userService.RegisterUser(user);
                return Ok(registerUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DTO.LoginResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var loginResponse = await _userService.Login(loginRequest.Username, loginRequest.Password);
                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            // For JWT tokens, logout is typically handled on the client side
            // by removing the token from storage. The server can maintain a blacklist
            // if needed, but for simplicity, we'll just return OK.
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(User))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var user = await _userService.GetUserById(userId);

                // Don't return password
                user.Password = null;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
