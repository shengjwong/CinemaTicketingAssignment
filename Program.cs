global using CinemaTicketingAssignment.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register SQL Server. The DbContext class is DB. DB.mdf will be placed at the root folder.
// Register SQL Server with an explicit database name to prevent auto-naming conflicts
builder.Services.AddDbContext<DB>(options =>
    options.UseSqlServer($@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={builder.Environment.ContentRootPath}\CinemaDB.mdf;Database=CinemaTicketDb;Integrated Security=True;Encrypt=False;"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();