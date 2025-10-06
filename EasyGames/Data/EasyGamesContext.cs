using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<EasyGames.Models.InventoryLocation> InventoryLocation { get; set; } = default!;
    public DbSet<EasyGames.Models.Inventory> Inventory { get; set; } = default!;
    public DbSet<EasyGames.Models.Customer> Customer { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
