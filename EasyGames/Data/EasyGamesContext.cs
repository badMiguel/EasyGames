using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace EasyGames.Data;

public class EasyGamesContext : IdentityDbContext<ApplicationUser>
{
    public EasyGamesContext(DbContextOptions<EasyGamesContext> options)
        : base(options) { }

    public DbSet<EasyGames.Models.Item> Item { get; set; } = default!;
    public DbSet<EasyGames.Models.Category> Category { get; set; } = default!;
    public DbSet<EasyGames.Models.ItemCategory> ItemCategory { get; set; } = default!;
    public DbSet<EasyGames.Models.Review> Review { get; set; } = default!;
    public DbSet<EasyGames.Models.Order> Order { get; set; } = default!;
    public DbSet<EasyGames.Models.OrderItem> OrderItem { get; set; } = default!;
    public DbSet<EasyGames.Models.Shop> Shop { get; set; } = default!;
    public DbSet<EasyGames.Models.Inventory> Inventory { get; set; } = default!;
    public DbSet<EasyGames.Models.Customer> Customer { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(o => o.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Shop)
            .WithMany(o => o.Orders)
            .HasForeignKey(o => o.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Inventory)
            .WithMany()
            .HasForeignKey(oi => oi.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(builder);
    }
}
