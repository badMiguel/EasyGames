using EasyGames.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Models;
using Microsoft.EntityFrameworkCore;

public class EasyGamesContext : DbContext
{
    public EasyGamesContext(DbContextOptions<EasyGamesContext> options)
        : base(options) { }

    public DbSet<EasyGames.Models.Item> Item { get; set; } = default!;

    public DbSet<EasyGames.Models.Category> Category { get; set; } = default!;
}
