using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DignaApi.Data;
using DignaApi.Services;
using Microsoft.OpenApi.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Use Startup pattern for both local and Lambda boot
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DignaDbContext>();
    dbContext.SeedAsync().GetAwaiter().GetResult();
}

// Configure middleware
startup.Configure(app, app.Environment);

// Log loaded assemblies (optional for diagnostics)
foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.GetName().Name))
{
    try
    {
        var name = asm.GetName();
        Console.WriteLine($"Loaded assembly: {name.Name}, Version={name.Version}, Location={asm.Location}");
    }
    catch { /* ignore dynamic assemblies */ }
}

app.Run();
