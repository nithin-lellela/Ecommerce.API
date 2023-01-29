using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _basketRepository;
        public BasketController(IBasketRepository basketRepository)
        {
            _basketRepository = basketRepository;
        }

        [HttpGet("Basket/{id}")]
        public async Task<IActionResult> GetBasketAsync(string id)
        {
            var basket = await _basketRepository.GetBasketAsync(id);
            if(basket != null)
            {
                return Ok(basket);
            }
            return BadRequest("Basket Not Found");
        }

        [HttpPost("Add")]
        public async Task<IActionResult> UpdateBasket([FromBody] CustomerBasket customerBasket)
        {
            var basket = await _basketRepository.UpdateBasketAsync(customerBasket);
            return Ok(basket);
        }

        [HttpDelete("Remove/{id}")]
        public async Task<IActionResult> DeleteBasket(string id)
        {
            var removeBasket = await _basketRepository.DeleteBasketAsync(id);
            if (!removeBasket)
            {
                return BadRequest("Error Occured while Deleting Basket in Redis");
            }
            return Ok("Successfully Removed Basket");
        }

    }
}
