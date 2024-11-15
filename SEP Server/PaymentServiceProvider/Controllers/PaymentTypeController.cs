using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentServiceProvider.DTO;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Controllers
{
    [Route("api/payment-types")]
    [ApiController]
    public class PaymentTypeController : ControllerBase
    {
        private readonly IPaymentTypeService _paymentTypeService;

        public PaymentTypeController(IPaymentTypeService paymentTypeService)
        {
            _paymentTypeService = paymentTypeService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PaymentType>))]
        public async Task<IActionResult> GetAllPaymentTypes()
        {
            return Ok(await _paymentTypeService.GetAllPaymentTypes());
        }

        [HttpGet("GetAllClientPayments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PaymentType>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllPaymentTypesByClientId(int clientId)
        {
            try
            {
                var paymentTypes = await _paymentTypeService.GetAllPaymentTypesByClientId(clientId);
                return Ok(paymentTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PaymentType>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPaymentType([FromBody] PaymentType paymentType)
        {
            try
            {
                var paymentTypes = await _paymentTypeService.AddPaymentType(paymentType);
                return Ok(paymentTypes);
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
        public async Task<IActionResult> RemovePaymentType(int id)
        {
            try
            {
                bool isDeleted = await _paymentTypeService.RemovePaymentType(id);

                if (!isDeleted)
                    return NotFound(); // Return 404 if the item was not found

                return NoContent(); // Return 204 No Content if successfully deleted
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddWebShopClientPaymentType")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebShopClientPaymentTypesDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddWebShopClientPaymentType([FromBody] WebShopClientPaymentTypesDto webShopClientPaymentType)
        {
            try
            {
                var webShopClientPaymentTypes = await _paymentTypeService.AddWebShopClientPaymentType(webShopClientPaymentType);
                return Ok(webShopClientPaymentTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("RemoveWebShopClientPaymentType")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveWebShopClientPaymentType(int clientId, int paymentId)
        {
            try
            {
                bool isDeleted = await _paymentTypeService.RemoveWebShopClientPaymentType(clientId, paymentId);

                if (!isDeleted)
                    return NotFound(); // Return 404 if the item was not found

                return NoContent(); // Return 204 No Content if successfully deleted
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
