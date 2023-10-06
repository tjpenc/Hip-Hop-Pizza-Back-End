using Microsoft.EntityFrameworkCore;
using HipHopPizzaBackend.Models;
public class HipHopPizzaDbContext : DbContext
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<PaymentType> PaymentTypes { get; set; }
    public DbSet<Revenue> Revenues { get; set; }
    public DbSet<User> Users { get; set; }

    public HipHopPizzaDbContext(DbContextOptions<HipHopPizzaDbContext> context) : base(context)
    {

    }
}
