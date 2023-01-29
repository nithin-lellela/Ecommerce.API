using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.RequestDTOs
{
    public class UserLoginDTO
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
