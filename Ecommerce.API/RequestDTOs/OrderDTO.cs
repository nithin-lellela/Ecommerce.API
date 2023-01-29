using System;

namespace Ecommerce.API.RequestDTOs
{
    public class OrderDTO
    {
        public string BasketId { get; set; }
        public Guid DeliveryMethodId { get; set; }
        public AddressDTO ShippingAddress { get; set; }
    }
}
