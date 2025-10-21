using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using Azure.Messaging.ServiceBus;
using log4net;
using Azure.Storage.Queues;
namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductContext _context;
        //private readonly ServiceBusClient _serviceBusClient;
        //private readonly string _queueName = "colanotificacion";
         private readonly QueueClient _queueClient;

        private static readonly ILog _log = LogManager.GetLogger(typeof(ProductsController));

        public ProductsController(ProductContext context
        //, ServiceBusClient serviceBusClient
        , QueueClient queueClient
        )
        {
            _context = context;
            //_serviceBusClient = serviceBusClient;
             _queueClient = queueClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
             _log.Info("✅ GET /api/products iniciado");
            var products = await _context.Products.ToListAsync();
             _log.Info($"✅ GET /api/products completado. Total={products.Count}");
            return Ok(products);
        }
        
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
             try
            {
                //Guardar el producto en la base de datos
                _log.Info($"✅ POST /api/products: creando '{product.Name}'");
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                //return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product); //devuelve una respuesta HTTP 201 Created después de crear un producto

                //Enviar notificación a la cola de Service Bus
                //var sender = _serviceBusClient.CreateSender(_queueName);
                //await sender.SendMessageAsync(new ServiceBusMessage($"Producto '{product.Name}' registrado correctamente."));

                //Enviar notificación a Azure Storage Queue
                var payload = $"Producto '{product.Name}' registrado correctamente. Id={product.Id}";
                await _queueClient.SendMessageAsync(payload);
                _log.Info($"☁️ STORAGE QUEQUE '{product.Name}' registrado en cola.");

                _log.Info($"✅POST /api/products: '{product.Name}' guardado y notificado a Service Bus.");
                return Ok(new { mensaje = "Producto registrado y notificación enviada." });
            }
            catch (Exception ex)
            {
                _log.Error("❌ Error en POST /api/products", ex);
                throw;
            }
        }
    }
}