using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PaymentType>))]
        public async Task<IActionResult> GetAllPaymentTypes()
        {
            return Ok(await _paymentTypeService.GetAllPaymentTypes());
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentType))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPaymentType([FromBody] PaymentType paymentType)
        {
            try
            {
                await _paymentTypeService.AddPaymentType(paymentType);
                var allPaymentTypes = await _paymentTypeService.GetAllPaymentTypes();
                return Ok(allPaymentTypes);
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
    }
}
