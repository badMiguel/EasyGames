using System;
using System.Linq;
using EasyGames.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyGames.Models;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new EasyGamesContext(
            serviceProvider.GetRequiredService<DbContextOptions<EasyGamesContext>>()
        );

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        {
            // ---- FOR USERS ----
            foreach (var role in Enum.GetNames(typeof(UserRoles)))
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var ownerEmail = "owner@email.com";
            var shopProprietorEmail = "shop@email.com";
            var customerEmail = "customer@email.com";

            var owner = await userManager.FindByEmailAsync(ownerEmail);
            var shopProprietor = await userManager.FindByEmailAsync(shopProprietorEmail);
            var customer = await userManager.FindByEmailAsync(customerEmail);

            var newCustomer = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                EmailConfirmed = true,
            };
            if (customer == null)
            {
                await userManager.CreateAsync(newCustomer, "Customer123.");
                await userManager.AddToRoleAsync(newCustomer, UserRoles.Customer.ToString());
            }
            else
            {
                if (!await userManager.IsInRoleAsync(customer, UserRoles.Customer.ToString()))
                {
                    await userManager.AddToRoleAsync(customer, UserRoles.Customer.ToString());
                }
            }

            var newShopProprietor = new ApplicationUser
            {
                UserName = shopProprietorEmail,
                Email = shopProprietorEmail,
                EmailConfirmed = true,
            };
            if (shopProprietor == null)
            {
                await userManager.CreateAsync(newShopProprietor, "Shop123.");
                await userManager.AddToRoleAsync(
                    newShopProprietor,
                    UserRoles.ShopProprietor.ToString()
                );
            }
            else
            {
                if (
                    !await userManager.IsInRoleAsync(
                        newShopProprietor,
                        UserRoles.ShopProprietor.ToString()
                    )
                )
                {
                    await userManager.AddToRoleAsync(
                        newShopProprietor,
                        UserRoles.ShopProprietor.ToString()
                    );
                }
            }

            var newOwner = new ApplicationUser
            {
                UserName = ownerEmail,
                Email = ownerEmail,
                EmailConfirmed = true,
            };
            if (owner == null)
            {
                await userManager.CreateAsync(newOwner, "Owner123.");
                await userManager.AddToRoleAsync(newOwner, UserRoles.Owner.ToString());
            }
            else
            {
                if (!await userManager.IsInRoleAsync(newOwner, UserRoles.Owner.ToString()))
                {
                    await userManager.AddToRoleAsync(newOwner, UserRoles.Owner.ToString());
                }
            }

            // ----  OTHERS  ----
            var books = new Category { Name = "Books" };
            var games = new Category { Name = "Games" };
            var toys = new Category { Name = "Toys" };
            if (context.Category.FirstOrDefault(c => c.Name == "Books") == null)
            {
                context.Category.Add(books);
                await context.SaveChangesAsync();
                ;
            }
            if (context.Category.FirstOrDefault(c => c.Name == "Games") == null)
            {
                context.Category.Add(games);
                await context.SaveChangesAsync();
                ;
            }
            if (context.Category.FirstOrDefault(c => c.Name == "Toys") == null)
            {
                context.Category.Add(toys);
                await context.SaveChangesAsync();
                ;
            }

            if (!context.Shop.Any())
            {
                var newShops = new List<Shop>
                {
                    new Shop
                    {
                        ShopName = "Online Shop",
                        ContactNumber = "+6123456789",
                        OwnerId = newOwner.Id,
                    },
                    new Shop
                    {
                        ShopName = "Tiny Shop",
                        ContactNumber = "+6987654321",
                        OwnerId = newShopProprietor.Id,
                    },
                };
                context.Shop.AddRange(newShops);
                await context.SaveChangesAsync();

                var newLocation = new List<InventoryLocation>
                {
                    new InventoryLocation
                    {
                        ShopId = newShops[0].ShopId,
                        LocationType = LocationTypes.Online,
                    },
                    new InventoryLocation
                    {
                        ShopId = newShops[1].ShopId,
                        LocationType = LocationTypes.Physical,
                        Address = "123 Big Street, Tiny City",
                    },
                };
                context.InventoryLocation.AddRange(newLocation);
                await context.SaveChangesAsync();

                context.Customer.AddRange(
                    new Customer { UserId = newCustomer.Id, IsGuest = false },
                    new Customer { UserId = newOwner.Id, IsGuest = false },
                    new Customer { UserId = newShopProprietor.Id, IsGuest = false },
                    new Customer { IsGuest = true }
                );
                await context.SaveChangesAsync();

                // Data generated with AI
                var newItems = new List<Item>
                {
                    new Item
                    {
                        Name = "The Great Gatsby",
                        BuyPrice = 8.99m,
                        SellPrice = 10.99m,
                        ProductionDate = new DateTime(2021, 2, 15),
                        Description = "Classic novel by F. Scott Fitzgerald",
                    },
                    new Item
                    {
                        Name = "Harry Potter and the Sorcerer's Stone",
                        BuyPrice = 10.99m,
                        SellPrice = 12.50m,
                        ProductionDate = new DateTime(2020, 2, 10),
                        Description = "Fantasy book by J.K. Rowling",
                    },
                    new Item
                    {
                        Name = "Atomic Habits",
                        BuyPrice = 13.00m,
                        SellPrice = 15.00m,
                        ProductionDate = new DateTime(2022, 2, 05),
                        Description = "Self-improvement book by James Clear",
                    },
                    new Item
                    {
                        Name = "The Legend of Zelda: Breath of the Wild",
                        BuyPrice = 50.00m,
                        SellPrice = 59.99m,
                        ProductionDate = new DateTime(2019, 2, 20),
                        Description = "Open-world adventure game for Nintendo Switch",
                    },
                    new Item
                    {
                        Name = "Minecraft",
                        BuyPrice = 23.00m,
                        SellPrice = 26.95m,
                        ProductionDate = new DateTime(2018, 2, 14),
                        Description = "Sandbox building game for multiple platforms",
                    },
                    new Item
                    {
                        Name = "Chess Set",
                        BuyPrice = 15.00m,
                        SellPrice = 20.00m,
                        ProductionDate = new DateTime(2021, 2, 10),
                        Description = "Classic strategy board game",
                    },
                    new Item
                    {
                        Name = "LEGO Classic Box",
                        BuyPrice = 41.99m,
                        SellPrice = 45.00m,
                        ProductionDate = new DateTime(2021, 2, 01),
                        Description = "Creative building blocks for all ages",
                    },
                    new Item
                    {
                        Name = "Barbie Doll",
                        BuyPrice = 14.99m,
                        SellPrice = 18.99m,
                        ProductionDate = new DateTime(2020, 2, 12),
                        Description = "Fashion doll with accessories",
                    },
                    new Item
                    {
                        Name = "Remote Control Car",
                        BuyPrice = 26.82m,
                        SellPrice = 29.99m,
                        ProductionDate = new DateTime(2022, 2, 22),
                        Description = "High-speed RC toy car with rechargeable battery",
                    },
                };
                context.Item.AddRange(newItems);
                await context.SaveChangesAsync();

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
                await context.SaveChangesAsync();

                var newReviews = new List<Review>
                {
                    new Review
                    {
                        ItemId = newItems[0].ItemId,
                        StarRating = 5,
                        UserId = newCustomer.Id,
                        Comment = "Wow! I like this!",
                    },
                    new Review
                    {
                        ItemId = newItems[1].ItemId,
                        StarRating = 4,
                        UserId = newCustomer.Id,
                        Comment = "Wow! I like this!",
                    },
                    new Review
                    {
                        ItemId = newItems[2].ItemId,
                        StarRating = 5,
                        UserId = newCustomer.Id,
                    },
                    new Review
                    {
                        ItemId = newItems[3].ItemId,
                        StarRating = 3,
                        UserId = newCustomer.Id,
                        Comment = "Its alright...",
                    },
                    new Review
                    {
                        ItemId = newItems[4].ItemId,
                        StarRating = 2,
                        UserId = newCustomer.Id,
                    },
                    new Review
                    {
                        ItemId = newItems[5].ItemId,
                        StarRating = 3,
                        UserId = newCustomer.Id,
                        Comment = "Its alright...",
                    },
                    new Review
                    {
                        ItemId = newItems[6].ItemId,
                        StarRating = 1,
                        UserId = newCustomer.Id,
                        Comment = "I hate this!",
                    },
                    new Review
                    {
                        ItemId = newItems[7].ItemId,
                        StarRating = 2,
                        UserId = newCustomer.Id,
                        Comment = "meh",
                    },
                    new Review
                    {
                        ItemId = newItems[8].ItemId,
                        StarRating = 3,
                        UserId = newCustomer.Id,
                    },
                };
                context.Review.AddRange(newReviews);
                await context.SaveChangesAsync();

                Random rnd = new Random();
                foreach (var item in newItems)
                {
                    context.Inventory.AddRange(
                        new Inventory
                        {
                            ItemId = item.ItemId,
                            LocationId = newLocation[0].InventoryLocationId,
                            Quantity = rnd.Next(1, 101),
                        },
                        new Inventory
                        {
                            ItemId = item.ItemId,
                            LocationId = newLocation[1].InventoryLocationId,
                            Quantity = rnd.Next(1, 101),
                        }
                    );
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
