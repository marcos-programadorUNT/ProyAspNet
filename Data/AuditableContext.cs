using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class AuditableContext : DbContext
    {
        public AuditableContext(DbContextOptions<AuditableContext> options) : base(options)
        {
        }
        public DbSet<Auditable> Auditables { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auditable>()
           .Property(o => o.Monto)
           .HasColumnType("decimal(18,2)");
        }
    }
}
