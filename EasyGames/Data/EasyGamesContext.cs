using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
    public DbSet<EasyGames.Models.UserItem> UserItem { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
