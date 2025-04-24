using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShinSerwis.Data;
using ShinSerwis.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.SetPostgresVersion(new Version(13, 0))
                                      .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<ShinSerwis.Services.Migrations.UpdatePasswordsService>();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

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

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var updatePasswordsService = scope.ServiceProvider
            .GetRequiredService<ShinSerwis.Services.Migrations.UpdatePasswordsService>();
        
        await updatePasswordsService.MigratePasswords();
        
        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedData();
    }
}

app.Run();
