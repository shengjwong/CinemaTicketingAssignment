using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. 注册 MVC 视图服务与 HttpClient (用于 reCAPTCHA 验证)
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// 2. 注册数据库上下文 (LocalDB)
builder.Services.AddDbContext<DB>(options =>
    options.UseSqlServer($@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={builder.Environment.ContentRootPath}\CinemaDB.mdf;Database=CinemaTicketingDb;Integrated Security=True;Encrypt=False"));

// 3. 配置 Cookie 身份验证 (指向分立的登录与拒绝路由)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";           // 普通用户默认登录页
        options.AccessDeniedPath = "/Account/AdminLogin"; // 权限不足/未登录管理员时自动跳转到管理员登录页
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

var app = builder.Build();

// 4. 确保数据库已被创建
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    db.Database.EnsureCreated();
}

// 5. 配置 HTTP 请求管道
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 认证与授权中间件顺序非常重要
app.UseAuthentication();
app.UseAuthorization();

// 6. 配置默认路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();