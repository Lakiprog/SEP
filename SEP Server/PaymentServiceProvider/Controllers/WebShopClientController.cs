using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/web-shop-client")]
    [ApiController]
    public class WebShopClientController : ControllerBase
    {
        private readonly IWebShopClientService _webShopClientService;

        public WebShopClientController(IWebShopClientService webShopClientService)
        {
            _webShopClientService = webShopClientService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WebShopClient>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWebShopClientById(int id)
        {
            try
            {
                var webShopClient = await _webShopClientService.GetWebShopClientById(id);
                return Ok(webShopClient);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WebShopClient>))]
        public async Task<IActionResult> GetAllWebShopClients()
        {
            try
            {
                var webShopClients = await _webShopClientService.GetAllWebShopClients();
                return Ok(webShopClients);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebShopClient))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddWebShopClient([FromBody] WebShopClient webShopClient)
        {
            try
            {
                var newWebShopClient = await _webShopClientService.AddWebShopClient(webShopClient);
                return Ok(newWebShopClient);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveWebShopClient(int id)
        {
            try
            {
                bool isDeleted = await _webShopClientService.RemoveWebShopClient(id);
                if (!isDeleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
