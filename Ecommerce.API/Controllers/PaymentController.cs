using Ecommerce.API.Helpers;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;
        public PaymentController(IPaymentService paymentService, IEmailService emailService, UserManager<User> userManager)
        {
            _paymentService = paymentService;
            _emailService = emailService;
            _userManager = userManager; 
        }

        [HttpPost("ProcessRequestOrder")]
        public async Task<IActionResult> ProcessRequestOrder([FromBody] PaymentRequest paymentRequest)
        {
            MerchantOrder merchantOrder = await _paymentService.ProcessMerchantOrder(paymentRequest);
            return Ok(merchantOrder);
        }

        [HttpPost("CompleteOrderProcess")]
        public async Task<IActionResult> CompleteOrderProcess([FromBody] OrderProcessRequest orderProcessRequest)
        {
            string paymentMessage = await _paymentService.CompleteOrderProcess(orderProcessRequest);
            if(paymentMessage == "captured")
            {
                //var userSubscriptionDetails = await _paymentService.UpdatePaymentCompletedInDB(orderProcessRequest);
                //var userSubscriptionDetails = await _paymentService.GetUserSubscriptionDetails(orderProcessRequest.UserId);
                return Ok(new AuthResponseModel(ResponseCode.Ok, "Success", orderProcessRequest));
            }
            else
            {
                return Ok(new AuthResponseModel(ResponseCode.BadRequest, "Failed", null));
            }
        }

        [HttpPut("UpdateSubscription")]
        public async Task<IActionResult> UpdateSubscription([FromBody] OrderProcessRequest orderProcessRequest)
        {
            var userSubscriptionDetails = await _paymentService.UpdatePaymentCompletedInDB(orderProcessRequest);
            if(userSubscriptionDetails != null)
            {
                var user = await _userManager.FindByIdAsync(orderProcessRequest.UserId);
                await _emailService.SendAsync("lellelanithin@gmail.com", user.Email, "Subscription Successfull",
                    $"You are successfully subscribed to abc.com\n please login to the application : https://localhost:4200/account/login");
                return Ok(new AuthResponseModel(ResponseCode.Ok, "Success", userSubscriptionDetails));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Failed", null));
        }

    }
}
