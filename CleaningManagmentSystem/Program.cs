using Microsoft.AspNetCore.Http;
using CleaningManagmentSystem.Data;
using CleaningManagmentSystem.Services;

// ── Kill any process already using port 5000 before we try to bind ─────────
try
{
    var psi = new System.Diagnostics.ProcessStartInfo("netstat", "-ano")
        { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
    using var proc = System.Diagnostics.Process.Start(psi)!;
    var lines = (await proc.StandardOutput.ReadToEndAsync()).Split('\n');
    foreach (var line in lines)
    {
        if (line.Contains(":5000") && line.Contains("LISTENING"))
        {
            var parts = line.Trim().Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[^1], out int pid) && pid > 0
                && pid != System.Diagnostics.Process.GetCurrentProcess().Id)
            {
                Console.WriteLine($"[Startup] Freeing port 5000 (killing PID {pid})...");
                try { System.Diagnostics.Process.GetProcessById(pid).Kill(entireProcessTree: true); }
                catch { /* already gone */ }
            }
        }
    }
    await Task.Delay(800); // let OS release the socket
}
catch { /* netstat not available — continue anyway */ }

var builder = WebApplication.CreateBuilder(args);

// Bind to port 5000 explicitly
builder.WebHost.UseUrls("http://0.0.0.0:5000");

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

Console.WriteLine("[Startup] Application running on http://0.0.0.0:5000 (all interfaces)");
app.Run("http://0.0.0.0:5000");