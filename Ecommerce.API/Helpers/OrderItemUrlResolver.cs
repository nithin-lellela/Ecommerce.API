using AutoMapper;
using Ecommerce.API.DTOs;
using Ecommerce.Core.Entities.OrderAggregate;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.API.Helpers
{
    public class OrderItemUrlResolver : IValueResolver<OrderItem, OrderItemDTO, string>
    {
        private readonly IConfiguration _configuration;
        public OrderItemUrlResolver(IConfiguration config)
        {
            _configuration = config;    
        }

        public string Resolve(OrderItem source, OrderItemDTO destination, string destMember, ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.ItemOrdered.PictureUrl))
            {
                return _configuration["ApiUrl"] + source.ItemOrdered.PictureUrl;
            }
            return null;
        }
    }
}
