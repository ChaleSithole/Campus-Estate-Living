using System.Globalization;
using CampusEstateLiving.Data;
using CampusEstateLiving.Models;
using CampusEstateLiving.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var za = CultureInfo.CreateSpecificCulture("en-ZA");
CultureInfo.DefaultThreadCurrentCulture = za;
CultureInfo.DefaultThreadCurrentUICulture = za;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var dbPath = ResolveSqlitePath(builder);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.Name = "cel.auth";
    options.Cookie.HttpOnly = true;
});

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EstateAccountService>();
builder.Services.AddScoped<OccupancyService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SchemaEnsure.ApplyAsync(db);
    await SeedData.InitialiseAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string ResolveSqlitePath(WebApplicationBuilder builder)
{
    var configured = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configured)
        && configured.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
        && !configured.Contains("SQLEXPRESS", StringComparison.OrdinalIgnoreCase)
        && !configured.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        var file = configured.Split("Data Source=", 2, StringSplitOptions.TrimEntries)[1]
            .Trim()
            .TrimEnd(';');
        if (!Path.IsPathRooted(file))
            file = Path.Combine(builder.Environment.ContentRootPath, file);
        return file;
    }

    var content = Path.Combine(builder.Environment.ContentRootPath, "campusestate.db");
    if (File.Exists(content)) return content;
    return Path.Combine(AppContext.BaseDirectory, "campusestate.db");
}
