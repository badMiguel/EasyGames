using System.Runtime.InteropServices;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
string connectionString;
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    connectionString = builder.Configuration.GetConnectionString("EasyGamesContext");
    builder.Services.AddDbContext<EasyGamesContext>(options =>
        options.UseSqlServer(connectionString)
    );
}
else
{
    connectionString = builder.Configuration.GetConnectionString("EasyGamesContextSqlite");
    builder.Services.AddDbContext<EasyGamesContext>(options => options.UseSqlite(connectionString));
}

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
