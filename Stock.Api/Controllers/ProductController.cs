﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Stock.Api.DTOs;
using Stock.AppService.Services;
using Stock.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Stock.Api.Extensions;

namespace Stock.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductService service;
        private readonly ProductTypeService productTypeService;
        private readonly IMapper mapper;
        private readonly ProviderService providerService;

        public ProductController(
            ProductService service, 
            ProductTypeService productTypeService, 
            IMapper mapper,
            ProviderService providerService)
        {
            this.service = service;
            this.productTypeService = productTypeService;
            this.mapper = mapper;
            this.providerService = providerService; 
        }

        /// <summary>
        /// Permite recuperar todas las instancias
        /// </summary>
        /// <returns>Una colección de instancias</returns>
        [HttpGet]
        public ActionResult<IEnumerable<ProductDTO>> Get()
        {
            try
            {
                var result = this.service.GetAll();
                if (result == null)
                {
                    return NotFound();
                }
                return this.mapper.Map<IEnumerable<ProductDTO>>(result).ToList();
            }
            catch(Exception)
            {
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Permite recuperar una instancia mediante un identificador
        /// </summary>
        /// <param name="id">Identificador de la instancia a recuperar</param>
        /// <returns>Una instancia</returns>
        [HttpGet("{id}")]
        public ActionResult<ProductDTO> Get(string id)
        {
            var result = this.service.Get(id);
            if (result == null)
            {
                return NotFound();
            }
            //return this.mapper.Map<ProductDTO>(this.service.Get(id));
            return Ok(this.mapper.Map<ProductDTO>(result));
        }

        /// <summary>
        /// Permite crear una nueva instancia
        /// </summary>
        /// <param name="value">Una instancia</param>
        [HttpPost]
        public ActionResult Post([FromBody] ProductDTO value)
        {
            TryValidateModel(value);

            try {
                var product = this.mapper.Map<Product>(value);
                product.ProductType = this.productTypeService.Get(value.ProductTypeId.ToString());
                product.Provider = this.providerService.Get(value.ProviderId.ToString());
                this.service.Create(product);
                value.Id = product.Id;
                return Ok(new { Success = true, Message = "", data = value });
            } catch {
                return Ok(new { Success = false, Message = "The name is already in use" });
            }
        }

        /// <summary>
        /// Permite editar una instancia
        /// </summary>
        /// <param name="id">Identificador de la instancia a editar</param>
        /// <param name="value">Una instancia con los nuevos datos</param>
        [HttpPut("{id}")]
        public ActionResult Put(string id, [FromBody] ProductDTO value)
        {
            var product = this.service.Get(id);
            TryValidateModel(value);
            if (product == null)
                return NotFound();

            this.mapper.Map<ProductDTO, Product>(value, product);
            product.ProductType = this.productTypeService.Get(value.ProductTypeId.ToString());
            product.Provider = this.providerService.Get(value.ProviderId.ToString());
            this.service.Update(product);
            return Ok(new { statusCode = "200", result = "Producto modificado exitosamente" });
        }

        [HttpPost("search")]
        public ActionResult Search([FromBody] ProductSearchDTO model)
        {
            Expression<Func<Product, bool>> filter = x => !string.IsNullOrWhiteSpace(x.Id);

            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                filter = filter.AndOrCustom(
                    x => x.Name.ToUpper().Contains(model.Name.ToUpper()),
                    model.Condition.Equals(ActionDto.AND));
            }

            if(!string.IsNullOrWhiteSpace(model.Brand))
            {
                filter = filter.AndOrCustom(
                    x => x.ProductType.Description.ToUpper().Contains(model.Brand.ToUpper()),
                    model.Condition.Equals(ActionDto.AND));
            }

            var products = this.service.Search(filter);
            return Ok(products);
        }

        /// <summary>
        /// Permite borrar una instancia
        /// </summary>
        /// <param name="id">Identificador de la instancia a borrar</param>
        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            try {
                var product = this.service.Get(id);
                this.service.Delete(product);
                return Ok(new { Success = true, Message = "", data = id });
            } catch {
                return Ok(new { Success = false, Message = "", data = id });
            }
        }

        /// <summary>
        /// Permite conocer el stock de un producto
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <returns>El stock disponible</returns>
        [HttpGet("stock/{id}")]
        public ActionResult<GenericResultDTO<int>> ObtenerStock(string id)
        {
            return new GenericResultDTO<int>(this.service.ObtenerStock(id));
        }

        /// <summary>
        /// Permite descontar una cantidad de stock a un producto
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <param name="value">La cantidad a descontar</param>
        [HttpPut("stock/descontar/{id}")]
        public void DescontarStock(string id, [FromBody] int value)
        {
            this.service.DescontarStock(id, value);
        }

        /// <summary>
        /// Permite sumar una cantidad de stock a un producto
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <param name="value">La cantidad a sumar</param>
        [HttpPut("stock/sumar/{id}")]
        public void SumarStock(string id, [FromBody] int value)
        {
            this.service.SumarStock(id, value);
        }

        /// <summary>
        /// Permite obtener el precio de venta al público de un producto
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <returns>El precio de venta al público</returns>
        [HttpGet("precioVenta/{id}")]
        public ActionResult<GenericResultDTO<decimal>> ObtenerPrecioVenta(string id)
        {
            return new GenericResultDTO<decimal>(this.service.ObtenerPrecioVentaPublico(id));
        }

        /// <summary>
        /// Permite obtener el precio de venta de un producto para un empleado
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <returns>El precio de venta para un empleado</returns>
        [HttpGet("precioVentaEmpleado/{id}")]
        public ActionResult<GenericResultDTO<decimal>> ObtenerPrecioVentaEmpleado(string id)
        {
            return new GenericResultDTO<decimal>(this.service.ObtenerPrecioVentaEmpleado(id));
        }
    }
}
