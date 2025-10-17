using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using Azure.Messaging.ServiceBus;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductContext _context;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly string _queueName = "colanotificacion";
        public ProductsController(ProductContext context, ServiceBusClient serviceBusClient)
        {
            _context = context;
            _serviceBusClient = serviceBusClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }
        
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            //Guardar el producto en la base de datos
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            //return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product); //devuelve una respuesta HTTP 201 Created después de crear un producto

            //Enviar notificación a la cola de Service Bus
             var sender = _serviceBusClient.CreateSender(_queueName);
            await sender.SendMessageAsync(new ServiceBusMessage($"Producto '{product.Name}' registrado correctamente."));

            return Ok(new { mensaje = "Producto registrado y notificación enviada." });
        }
    }
}