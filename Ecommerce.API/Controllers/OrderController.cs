using AutoMapper;
using Ecommerce.API.DTOs;
using Ecommerce.API.Helpers;
using Ecommerce.API.RequestDTOs;
using Ecommerce.Core.Entities.OrderAggregate;
using Ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        public OrderController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrderAsync([FromBody] OrderDTO orderDTO)
        {
            var email = HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value;
            var address = _mapper.Map<AddressDTO, Ecommerce.Core.Entities.OrderAggregate.Address>(orderDTO.ShippingAddress);
            var order = await _orderService.CreateOrderAsync(email, orderDTO.DeliveryMethodId, address, orderDTO.BasketId);
            if(order == null)
            {
                return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Failed to create order", null));
            }
            return Ok(new AuthResponseModel(ResponseCode.Ok, "Order Created Successfully", order));
        }

        [Authorize]
        [HttpGet("UserOrders")]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDTO>>> GetOrdersByUserAsync()
        {
            var email = HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value;
            var orders = await _orderService.GetOrdersForUserAsync(email);
            var orderToReturnDTO = _mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDTO>>(orders);
            return Ok(new AuthResponseModel(ResponseCode.Ok, "List of Orders", orderToReturnDTO));
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderToReturnDTO>> GetOrderByIdAsync(Guid id)
        {
            var email = HttpContext.User?.FindFirstValue(ClaimTypes.Email);
            var order = await _orderService.GetOrderByIdAsync(id, email);
            if(order == null)
            {
                return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Order Not Found", null));
            }
            return Ok(new AuthResponseModel(ResponseCode.Ok, "Order Exists", 
                _mapper.Map<Order, OrderToReturnDTO>(order)));
        }

        [HttpGet("DeliveryMethods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethodsAsync()
        {
            var email = HttpContext.User?.FindFirstValue(ClaimTypes.Email);
            var deliveryMethods = await _orderService.GetDeliveryMethodsAsync();
            return Ok(new AuthResponseModel(ResponseCode.Ok, "List Of Delivery Methods", deliveryMethods));
        }

    }
}
