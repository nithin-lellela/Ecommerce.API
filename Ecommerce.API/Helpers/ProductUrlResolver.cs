using AutoMapper;
using Ecommerce.API.DTOs;
using Ecommerce.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.API.Helpers
{
    public class ProductUrlResolver : IValueResolver<Product, ProductDTO, string>
    {
        private readonly IConfiguration _configuration;
        public ProductUrlResolver(IConfiguration config)
        {
            _configuration = config;    
        }

        public string Resolve(Product source, ProductDTO destination, string destMember, ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.PictureUrl))
            {
                return _configuration["ApiUrl"] + source.PictureUrl;
            }
            return null;
        }
    }
}
