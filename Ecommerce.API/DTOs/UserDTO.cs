﻿namespace Ecommerce.API.DTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool isAdmin { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
    }
}
