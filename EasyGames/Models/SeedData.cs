using System;
using System.Linq;
using EasyGames.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyGames.Models;

public static class SeedData
{
    public static async void Initialize(IServiceProvider serviceProvider)
    {
        using var context = new EasyGamesContext(
            serviceProvider.GetRequiredService<DbContextOptions<EasyGamesContext>>()
        );

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        {
            // ---- FOR USERS ----
            foreach (var role in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@email.com";
            var userEmail = "user@email.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            var user = await userManager.FindByEmailAsync(userEmail);

            var newUser = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
            };
            if (user == null)
            {
                await userManager.CreateAsync(newUser, "User123.");
                await userManager.AddToRoleAsync(newUser, "User");
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, "User"))
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };
            if (admin == null)
            {
                await userManager.CreateAsync(newAdmin, "Admin123.");
                await userManager.AddToRoleAsync(newAdmin, "Admin");
            }
            else
            {
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // ----  OTHERS  ----
            var books = new Category { Name = "Books" };
            var games = new Category { Name = "Games" };
            var toys = new Category { Name = "Toys" };
            if (context.Category.FirstOrDefault(c => c.Name == "Books") == null)
            {
                context.Category.Add(books);
                context.SaveChanges();
            }
            if (context.Category.FirstOrDefault(c => c.Name == "Games") == null)
            {
                context.Category.Add(games);
                context.SaveChanges();
            }
            if (context.Category.FirstOrDefault(c => c.Name == "Toys") == null)
            {
                context.Category.Add(toys);
                context.SaveChanges();
            }

            if (!context.Item.Any())
            {
                // Data generated with AI
                var newItems = new List<Item>
                {
                    new Item
                    {
                        Name = "The Great Gatsby",
                        Price = 10.99m,
                        ProductionDate = new DateTime(2021, 2, 15),
                        Description = "Classic novel by F. Scott Fitzgerald",
                        StockAmount = 25,
                    },
                    new Item
                    {
                        Name = "Harry Potter and the Sorcerer's Stone",
                        Price = 12.50m,
                        ProductionDate = new DateTime(2020, 2, 10),
                        Description = "Fantasy book by J.K. Rowling",
                        StockAmount = 40,
                    },
                    new Item
                    {
                        Name = "Atomic Habits",
                        Price = 15.00m,
                        ProductionDate = new DateTime(2022, 2, 05),
                        Description = "Self-improvement book by James Clear",
                        StockAmount = 30,
                    },
                    new Item
                    {
                        Name = "The Legend of Zelda: Breath of the Wild",
                        Price = 59.99m,
                        ProductionDate = new DateTime(2019, 2, 20),
                        Description = "Open-world adventure game for Nintendo Switch",
                        StockAmount = 15,
                    },
                    new Item
                    {
                        Name = "Minecraft",
                        Price = 26.95m,
                        ProductionDate = new DateTime(2018, 2, 14),
                        Description = "Sandbox building game for multiple platforms",
                        StockAmount = 50,
                    },
                    new Item
                    {
                        Name = "Chess Set",
                        Price = 20.00m,
                        ProductionDate = new DateTime(2021, 2, 10),
                        Description = "Classic strategy board game",
                        StockAmount = 35,
                    },
                    new Item
                    {
                        Name = "LEGO Classic Box",
                        Price = 45.00m,
                        ProductionDate = new DateTime(2021, 2, 01),
                        Description = "Creative building blocks for all ages",
                        StockAmount = 60,
                    },
                    new Item
                    {
                        Name = "Barbie Doll",
                        Price = 18.99m,
                        ProductionDate = new DateTime(2020, 2, 12),
                        Description = "Fashion doll with accessories",
                        StockAmount = 25,
                    },
                    new Item
                    {
                        Name = "Remote Control Car",
                        Price = 29.99m,
                        ProductionDate = new DateTime(2022, 2, 22),
                        Description = "High-speed RC toy car with rechargeable battery",
                        StockAmount = 20,
                    },
                };
                context.Item.AddRange(newItems);
                context.SaveChanges();

                context.ItemCategory.AddRange(
                    new ItemCategory { ItemId = newItems[0].ItemId, CategoryId = books.CategoryId },
                    new ItemCategory { ItemId = newItems[1].ItemId, CategoryId = books.CategoryId },
                    new ItemCategory { ItemId = newItems[2].ItemId, CategoryId = books.CategoryId },
                    new ItemCategory { ItemId = newItems[3].ItemId, CategoryId = games.CategoryId },
                    new ItemCategory { ItemId = newItems[4].ItemId, CategoryId = games.CategoryId },
                    new ItemCategory { ItemId = newItems[5].ItemId, CategoryId = games.CategoryId },
                    new ItemCategory { ItemId = newItems[6].ItemId, CategoryId = toys.CategoryId },
                    new ItemCategory { ItemId = newItems[7].ItemId, CategoryId = toys.CategoryId },
                    new ItemCategory { ItemId = newItems[8].ItemId, CategoryId = toys.CategoryId }
                );
                context.SaveChanges();

                var newReviews = new List<Review>
                {
                    new Review { ItemId = newItems[0].ItemId, StarRating = 5, UserId = newUser.Id, Comment = "Wow! I like this!" },
                    new Review { ItemId = newItems[1].ItemId, StarRating = 4, UserId = newUser.Id, Comment = "Wow! I like this!" },
                    new Review { ItemId = newItems[2].ItemId, StarRating = 5, UserId = newUser.Id},
                    new Review { ItemId = newItems[3].ItemId, StarRating = 3, UserId = newUser.Id, Comment = "Its alright..." },
                    new Review { ItemId = newItems[4].ItemId, StarRating = 2, UserId = newUser.Id },
                    new Review { ItemId = newItems[5].ItemId, StarRating = 3, UserId = newUser.Id, Comment = "Its alright..." },
                    new Review { ItemId = newItems[6].ItemId, StarRating = 1, UserId = newUser.Id, Comment = "I hate this!" },
                    new Review { ItemId = newItems[7].ItemId, StarRating = 2, UserId = newUser.Id, Comment= "meh" },
                    new Review { ItemId = newItems[8].ItemId, StarRating = 3, UserId = newUser.Id },
                };
                context.Review.AddRange(newReviews);
                context.SaveChanges();
            }
        }
    }
}
