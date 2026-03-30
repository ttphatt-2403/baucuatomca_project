using BauCuaTomCa.Data;
using BauCuaTomCa.Hubs;
using BauCuaTomCa.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        var connectionUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var connectionString = connectionUrl;
        
        // Render delivers the DB URL as postgres://user:pass@host/db
        if (!string.IsNullOrEmpty(connectionUrl) && (connectionUrl.StartsWith("postgres://") || connectionUrl.StartsWith("postgresql://")))
        {
            var databaseUri = new Uri(connectionUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
            connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;TrustServerCertificate=True;";
        }
        
        options.UseNpgsql(connectionString);
    }
});

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Firebase + Game services
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddSingleton<GameService>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR Hub
app.MapHub<GameHub>("/gameHub");

// Auto migrate/create on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        db.Database.Migrate(); // Use SQL Server Migrations for local dev
    }
    else
    {
        db.Database.EnsureCreated(); // Auto create tables for PostgreSQL on Render
    }
}

app.Run();
