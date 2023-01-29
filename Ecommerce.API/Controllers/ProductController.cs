using AutoMapper;
using Ecommerce.API.DTOs;
using Ecommerce.API.Helpers;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IGenericRepository<Product> _productRepo;

        private readonly IGenericRepository<ProductBrand> _productBrandRepo;

        private readonly IGenericRepository<ProductType> _productTypeRepo;
        private readonly  IMapper _mapper;

        public ProductController(IGenericRepository<Product> productRepo, IGenericRepository<ProductBrand> productBrandRepo, 
            IGenericRepository<ProductType> productTypeRepo, IMapper mapper)
        {
            _productRepo = productRepo;
            _productBrandRepo = productBrandRepo;
            _productTypeRepo = productTypeRepo;
            _mapper = mapper;
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductSpecParams productSpecParams)
        {
            var spec = new GetProductsWithTypeAndBrandSpecification(productSpecParams);

            var countSpec = new ProductsWithFiltersForCountSpecification(productSpecParams);
            
            var totalCount = await _productRepo.CountAsync(countSpec);
            
            var products = await _productRepo.ListAsync(spec);
            
            var data = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductDTO>>(products);
            
            return Ok(new Pagination<ProductDTO>(productSpecParams.PageIndex, productSpecParams.PageSize, totalCount, data));
        }

        [HttpGet("GetProduct/{id}")]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            var spec = new GetProductsWithTypeAndBrandSpecification(id);
            var product = await _productRepo.GetEntityWithSpec(spec);
            if(product != null)
            {
                return Ok(_mapper.Map<Product, ProductDTO>(product));
            }
            return BadRequest("Product Not Found");
        }

        [HttpGet("GetBrands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _productBrandRepo.ListAllAsync();
            return Ok(brands);
        }

        [HttpGet("GetTypes")]
        public async Task<IActionResult> GetTypes()
        {
            var types = await _productTypeRepo.ListAllAsync();
            return Ok(types);
        }

    }
}
