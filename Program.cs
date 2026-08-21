using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. 注册 MVC 视图服务与 HttpClient (用于 reCAPTCHA 验证)
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
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
    // EnsureCreated only creates a brand-new database. Existing LocalDB .mdf files
    // need this non-destructive schema upgrade before the seat-reservation feature
    // can query its tables.
    db.Database.ExecuteSqlRaw("""
        IF COL_LENGTH('dbo.Bookings', 'PaymentStatus') IS NULL
            ALTER TABLE dbo.Bookings ADD PaymentStatus nvarchar(30) NOT NULL CONSTRAINT DF_Bookings_PaymentStatus DEFAULT 'Paid';
        IF COL_LENGTH('dbo.Bookings', 'PaymentReference') IS NULL
            ALTER TABLE dbo.Bookings ADD PaymentReference nvarchar(50) NOT NULL CONSTRAINT DF_Bookings_PaymentReference DEFAULT '';

        IF OBJECT_ID(N'dbo.SeatHolds', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SeatHolds (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeatHolds PRIMARY KEY,
                ShowtimeId nvarchar(10) NOT NULL,
                SeatCode nvarchar(6) NOT NULL,
                UserId nvarchar(10) NOT NULL,
                ExpiresAtUtc datetime2 NOT NULL
            );
        END;

        IF OBJECT_ID(N'dbo.BookingSeats', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.BookingSeats (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingSeats PRIMARY KEY,
                BookingId nvarchar(10) NOT NULL,
                ShowtimeId nvarchar(10) NOT NULL,
                SeatCode nvarchar(6) NOT NULL,
                CONSTRAINT FK_BookingSeats_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id) ON DELETE CASCADE
            );
        END;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SeatHolds_ShowtimeId_SeatCode' AND object_id = OBJECT_ID(N'dbo.SeatHolds'))
            CREATE UNIQUE INDEX IX_SeatHolds_ShowtimeId_SeatCode ON dbo.SeatHolds(ShowtimeId, SeatCode);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BookingSeats_ShowtimeId_SeatCode' AND object_id = OBJECT_ID(N'dbo.BookingSeats'))
            CREATE UNIQUE INDEX IX_BookingSeats_ShowtimeId_SeatCode ON dbo.BookingSeats(ShowtimeId, SeatCode);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BookingSeats_BookingId' AND object_id = OBJECT_ID(N'dbo.BookingSeats'))
            CREATE INDEX IX_BookingSeats_BookingId ON dbo.BookingSeats(BookingId);
        """);
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
