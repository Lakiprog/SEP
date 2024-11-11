using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telecom.Interfaces;
using Telecom.Models;

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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegisterUser([FromBody] User user)
        {
            try
            {
                var registerUser = await _userService.RegisterUser(user);
                return Ok(registerUser);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
