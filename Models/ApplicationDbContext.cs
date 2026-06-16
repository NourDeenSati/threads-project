using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FirstApi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .Property(o => o.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10 },
                new Product { Id = 2, Name = "Headphones", Price = 150.00m, StockQuantity = 25 },
                new Product { Id = 3, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 15 },
                new Product { Id = 4, Name = "Gaming Mouse", Price = 70.00m, StockQuantity = 20 }
            );
        }
    }
}
