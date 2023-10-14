using Microsoft.EntityFrameworkCore;
using HipHopPizzaBackend.Models;
using System.Reflection.Metadata;
using Microsoft.Extensions.Hosting;

public class HipHopPizzaDbContext : DbContext
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<PaymentType> PaymentTypes { get; set; }
    public DbSet<Revenue> Revenues { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public HipHopPizzaDbContext(DbContextOptions<HipHopPizzaDbContext> context) : base(context)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(e => e.Items)
            .WithMany(e => e.Orders)
            .UsingEntity<OrderItem>();
    }
}
