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
    public class AuditableController : ControllerBase
    {
        private readonly AuditableContext _context;


        public AuditableController(AuditableContext context
        )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auditable>>> GetProducts()
        {
          
            var auditables = await _context.Auditables.ToListAsync();
            return Ok(auditables);
        }

        
        [HttpPost]
        public async Task<ActionResult<Auditable>> PostAuditable(Auditable product)
        {
             try
            {
                //Guardar auditable en la base de datos
                _context.Auditables.Add(product);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Auditable registrado correctamente." });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}