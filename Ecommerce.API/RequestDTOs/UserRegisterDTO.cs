using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.RequestDTOs
{
    public class UserRegisterDTO
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        [Required]
        [DataType(dataType: DataType.Password)]
        public string Password { get; set; }
        [Required]
        [MaxLength(10), MinLength(10)]
        public string PhoneNumber { get; set; }

    }
}
