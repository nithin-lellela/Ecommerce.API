using AutoMapper;
using Ecommerce.API.DTOs;
using Ecommerce.API.Helpers;
using Ecommerce.API.RequestDTOs;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Specifications;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _dbContext;
        public UserController(UserManager<User> userManager, IEmailService emailService, IConfiguration configuration,
            IMapper mapper, ApplicationDbContext dbContext, IPaymentService paymentService)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _mapper = mapper;
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLoginRequest)
        {
            if (ModelState.IsValid)
            {
                var isUserExists = await _userManager.FindByEmailAsync(userLoginRequest.Email);
                if(isUserExists != null)
                {
                    var user = await _userManager.CheckPasswordAsync(isUserExists, userLoginRequest.Password);
                    if (user)
                    {
                        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(isUserExists);
                        if (isEmailConfirmed)
                        {
                            var userDTO = new UserDTO()
                            {
                                Id = isUserExists.Id,
                                Name = isUserExists.Name,
                                Email = isUserExists.Email,
                                isAdmin = isUserExists.IsAdmin,
                                UserName = isUserExists.UserName,
                                Token = GenerateToken(isUserExists),
                            };
                            var userSubscriptionDetails = await _paymentService.GetUserSubscriptionDetails(isUserExists.Id);
                            if(userSubscriptionDetails != null && userSubscriptionDetails.IsSubscribed && userSubscriptionDetails.PaymentStatus == PaymentStatus.Completed)
                            {
                                
                                return Ok(new AuthResponseModel(ResponseCode.Ok, "Login Successfull", userDTO));
                            }
                            return Ok(new AuthResponseModel(ResponseCode.UnAuthorized, "Please subscribe in order to access the products", userDTO));
                        }
                        return BadRequest(new AuthResponseModel(ResponseCode.UnAuthorized, "Please check your email to verify your email address", isUserExists.Email));
                    }
                    return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Invalid Password", null));
                }
                return BadRequest(new AuthResponseModel(ResponseCode.NotFound, "User Not Found", null));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.NotFound, "All Fields are required", null));
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO userDTO)
        {
            if (ModelState.IsValid)
            {
                var isUserExists = await _userManager.FindByEmailAsync(userDTO.Email);
                if (isUserExists == null)
                {
                    var user = new User()
                    {
                        Email = userDTO.Email,
                        Name = userDTO.FirstName + " " + userDTO.LastName,
                        DateCreated = System.DateTime.Now,
                        PhoneNumber = userDTO.PhoneNumber,
                        IsAdmin = false,
                        UserName = userDTO.FirstName.ToLower() + userDTO.LastName.ToLower(),
                    };
                    var isUserCreated = await _userManager.CreateAsync(user, userDTO.Password);
                    if (isUserCreated.Succeeded)
                    {
                        var user1 = await _userManager.FindByEmailAsync(user.Email);
                        var address = new Address()
                        {
                            Id = Guid.NewGuid(),
                            AppUserId = user1.Id
                        };
                        await _dbContext.Address.AddAsync(address);
                        await _dbContext.SaveChangesAsync();
                        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user1);
                        var confirmationLink = $"https://localhost:4200/account/confirmemail?Id={user1.Id}&Token={confirmationToken}";
                        await _emailService.SendAsync("lellelanithin@gmail.com", user1.Email, "Please Confirm your email",
                            $"Please click on the link to confirm your email address:\n {confirmationLink}");

                        var userDto = new UserDTO()
                        {
                            Id = user1.Id,
                            Name = user1.Name,
                            Email = user1.Email,
                            isAdmin = user1.IsAdmin,
                            UserName = user1.UserName,
                            Token = GenerateToken(user1),
                        };
                        return Ok(new AuthResponseModel(ResponseCode.Ok, $"Please check your email: {user1.Email} to verify", userDto));
                    }
                    return BadRequest(new AuthResponseModel(ResponseCode.BadRequest,
                        string.Join("\n", isUserCreated.Errors.Select(x => x.Description).ToArray()) , null));
                }
                return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Email Already Exists\nPlease Login", null));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "All Fields are required", null));
        }

        [HttpGet("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromQuery] VerifyEmailParams verifyEmailParams)
        {
            verifyEmailParams.Token = verifyEmailParams.Token.Replace(" ", "+");
            var user = await _userManager.FindByIdAsync(verifyEmailParams.Id);
            if(user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, verifyEmailParams.Token);
                if (result.Succeeded)
                {
                    var userDTO = new UserDTO()
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = user.UserName,
                        Name = user.Name,
                        isAdmin = user.IsAdmin,
                        Token = GenerateToken(user)
                    };
                    return Ok(new AuthResponseModel(ResponseCode.Ok, "Email Address verified successfully", userDTO));
                    //return Ok("Email Address is successfully confirmed, now you can login with your credentials");
                }
                return BadRequest("Internal server error !!");
            }
            return BadRequest("Failed to Validate Email");
        }

        [HttpPost("SendVerificationLink")]
        public async Task<IActionResult> ConfirmationLink([FromBody] ConfirmEmailParams emailParams)
        {
            var isUserExists = await _userManager.FindByEmailAsync(emailParams.Email);
            if(isUserExists != null)
            {
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(isUserExists);
                var confirmationLink = $"https://localhost:4200/account/confirmemail?Id={isUserExists.Id}&Token={confirmationToken}";
                await _emailService.SendAsync("lellelanithin@gmail.com", emailParams.Email, "Please Confirm your email",
                            $"Please click on the link to confirm your email address:\n {confirmationLink}");
                return Ok(new AuthResponseModel(ResponseCode.Ok, $"Please check your email: {emailParams.Email} to verify", null));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.NotFound, "User Not Found", null));
        }

        private string GenerateToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("JwtConfig:SecretKey").Value);
            
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.NameId, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Expires = DateTime.Now.AddHours(2),
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        [HttpPost("ResetPasswordToken")]
        public async Task<IActionResult> ResetPasswordToken([FromBody] string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if(user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var confirmationLink = $"https://localhost:44318/api/User/ResetPassword/";
                return Ok(new AuthResponseModel(ResponseCode.Ok, "Reset Password token generated", token));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.NotFound, "User doesn't exist", null));
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            if (ModelState.IsValid)
            {
                var isUserExists = await _userManager.FindByEmailAsync(resetPasswordDTO.Email);
                if(isUserExists != null)
                {
                    if(string.Compare(resetPasswordDTO.Password, resetPasswordDTO.ConfirmPassword) == 0)
                    {
                        if (!string.IsNullOrEmpty(resetPasswordDTO.Token))
                        {
                            var passwordReset = await _userManager.ResetPasswordAsync(isUserExists, resetPasswordDTO.Token, resetPasswordDTO.Password);
                            if (passwordReset.Succeeded)
                            {
                                return Ok(new AuthResponseModel(ResponseCode.Ok, "Password Reset Successfull", null));
                            }
                            return BadRequest(new AuthResponseModel(
                                ResponseCode.BadRequest, string.Join(",", passwordReset.Errors.Select(x => x.Description).ToList()),
                                null ));
                        }
                        return BadRequest(new AuthResponseModel(ResponseCode.UnAuthorized, "Invalid Token", null));
                    }
                    return BadRequest(new AuthResponseModel(ResponseCode.BadRequest, "Passwords Doesn't match", null));
                }
                return BadRequest(new AuthResponseModel(ResponseCode.NotFound, "User Not Found", null));
            }
            return BadRequest("All Fields are required");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);
            if(user != null)
            {
                var userDTO = new UserDTO()
                {
                    Email = user.Email,
                    Id = user.Id,
                    Name = user.Name,
                    UserName = user.UserName,
                    isAdmin = user.IsAdmin,
                    Token = GenerateToken(user)
                };
                var userSubscriptionDetails = await _paymentService.GetUserSubscriptionDetails(user.Id);
                if(userSubscriptionDetails != null && userSubscriptionDetails.IsSubscribed && userSubscriptionDetails.PaymentStatus == PaymentStatus.Completed)
                {
                    return Ok(new AuthResponseModel(ResponseCode.Ok, "Current User is subscribed member", userDTO));
                }
                return Ok(new AuthResponseModel(ResponseCode.UnAuthorized, "Please subscribe in-order to access the products", userDTO));
            }
            return BadRequest(new AuthResponseModel(ResponseCode.UnAuthorized
                , "User Not Found or UnAuthorized", null));
        }

        [HttpPut("Address")]
        [Authorize]
        public async Task<IActionResult> UpdateUserAddress(AddressDTO addressDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);
            if(user != null)
            {
                var address = await _dbContext.Address.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == user.Id);    
                if(address == null)
                {
                    var addAddress = new Address()
                    {
                        Id = Guid.NewGuid(),
                        FirstName = addressDTO.FirstName,
                        LastName = addressDTO.LastName,
                        Street = addressDTO.Street,
                        ZipCode = addressDTO.ZipCode,
                        State = addressDTO.State,
                        City = addressDTO.City,
                        AppUserId = user.Id
                    };
                    await _dbContext.Address.AddAsync(address);
                    await _dbContext.SaveChangesAsync();
                    return Ok(_mapper.Map<Address, AddressDTO>(addAddress));
                }
                var updatedAddress = new Address()
                {
                    Id = address.Id,
                    FirstName = addressDTO.FirstName,
                    LastName = addressDTO.LastName,
                    Street = addressDTO.Street,
                    ZipCode = addressDTO.ZipCode,
                    State = addressDTO.State,
                    City = addressDTO.City,
                    AppUserId = user.Id
                };
                _dbContext.Address.Update(updatedAddress);
                await _dbContext.SaveChangesAsync();
                return Ok(_mapper.Map<Address, AddressDTO>(updatedAddress));
            }
            return BadRequest("User Not Found");
        }

        [HttpGet("GetAddress")]
        [Authorize]
        public async Task<IActionResult> GetUserAddress()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);
            var address = await _dbContext.Address.FirstOrDefaultAsync(x => x.AppUserId == user.Id);
            if(address == null)
            {
                return BadRequest("Address Not Found !");
            }
            return Ok(_mapper.Map<Address, AddressDTO>(address));   
        }

    }
}
