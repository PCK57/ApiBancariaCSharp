using Microsoft.EntityFrameworkCore;
using Project1.Controllers; // Para poder usar ModeloCuenta

namespace Project1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Esto le dice a EF Core: "Crea una tabla en SQL basada en nuestro modelo"
        public DbSet<ModeloCuenta> Cuentas { get; set; }
    }
}
