using Microsoft.AspNetCore.Http;
using CleaningManagmentSystem.Data;
using CleaningManagmentSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind URL (Render sets PORT, local dev uses 5000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Build connection string from env vars (Render) or use appsettings (local)
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME")
    ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE")
    ?? "yeka_cleaning";
var dbUser = Environment.GetEnvironmentVariable("DB_USER")
    ?? Environment.GetEnvironmentVariable("MYSQL_USER")
    ?? "yeka_user";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? Environment.GetEnvironmentVariable("MYSQL_PASSWORD")
    ?? "";

if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};SslMode=Required;AllowPublicKeyRetrieval=true;";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;
    Console.WriteLine($"[Startup] Using DB: {dbHost}:{dbPort}/{dbName}");
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Added for Mobile API
builder.Services.AddSingleton<EmailService>();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add session with explicit cookie settings
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Yeka.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Initialize and seed database
Console.WriteLine("[Startup] Initializing database...");
try
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        await DatabaseSeeder.SeedAsync(connectionString);
        Console.WriteLine("[Startup] Database initialization completed");
    }
    else
    {
        Console.WriteLine("[Startup] Warning: DefaultConnection not configured in appsettings.json");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Database initialization error: {ex.Message}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll"); // Enable CORS
app.UseSession();


app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("UserId");
    var userName = context.Session.GetString("UserName");
    Console.WriteLine($"[Request] {context.Request.Path} - UserId: {userId}, UserName: {userName}");
    await next();
});

app.UseAuthorization();

// Redirect old /Dashboard/Staff to /Dashboard/Staff/Index
app.MapGet("/Dashboard/Staff", (HttpContext context) =>
{
    context.Response.Redirect("/Dashboard/Staff/Index");
    return Results.Empty;
});

app.MapRazorPages();
app.MapControllers(); // Added for Mobile API

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Console.WriteLine($"[Startup] Application running on http://0.0.0.0:{port} (all interfaces)");
app.Run();