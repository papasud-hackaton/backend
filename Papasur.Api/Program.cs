using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Papasur.Api.Middleware;
using Papasur.Application;
using Papasur.Infrastructure;
using Papasur.Infrastructure.Persistence;
using Serilog;

// Bootstrap logger (hasta que se lea la config completa).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging estructurado con Serilog (config en appsettings "Serilog").
    builder.Host.UseSerilog((context, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Errores: Result para los de negocio; ProblemDetails 500 para los inesperados.
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health check real: verifica conexión a Postgres, no sólo que el proceso viva.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("postgres");

    // CORS por env: Cors__AllowedOrigins__0, __1, ... En Development sin config se permite localhost.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback).AllowAnyHeader().AllowAnyMethod();
        }
        // Sin orígenes configurados fuera de Development: política vacía (fail-closed).
    }));

    // Auth JWT (HS256, key simétrica >= 32 bytes por env Jwt__SymmetricKey).
    // En Development sin key se usa una fija de desarrollo; fuera de Development es obligatoria.
    var jwtKey = builder.Configuration["Jwt:SymmetricKey"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Falta Jwt__SymmetricKey (>= 32 bytes) fuera de Development.");
        }

        jwtKey = "dev-only-insecure-symmetric-key-0123456789";
    }

    // Se escribe de vuelta en la config para que JwtTokenGenerator (Infrastructure) firme
    // con EXACTAMENTE la misma key con la que la API valida — una sola fuente de verdad.
    builder.Configuration["Jwt:SymmetricKey"] = jwtKey;

    if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    {
        throw new InvalidOperationException("Jwt__SymmetricKey debe tener al menos 32 bytes.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "papasur",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "papasur",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                // Claims que emite JwtTokenGenerator; RoleClaimType es lo que consume [AuthorizeRoles].
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Admin", policy => policy.RequireRole("admin"));

    // Rate limiting global (fixed window por IP) con 429 explícito.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100),
                    Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:WindowSeconds", 10)),
                    QueueLimit = 0,
                }));
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Migraciones automáticas al arrancar (aplica las migraciones EF pendientes).
    // Controlado por Ef:AutoMigrate (env Ef__AutoMigrate); ON por defecto en Development.
    var autoMigrate = app.Configuration.GetValue("Ef:AutoMigrate", app.Environment.IsDevelopment());
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Admin inicial (sólo si no hay ningún usuario): sin esto nadie podría loguearse
        // para dar de alta el primero. Credenciales por Seed__AdminEmail/Seed__AdminPassword.
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(app.Environment.IsDevelopment());
    }

    if (app.Environment.IsDevelopment())
    {
        // Documento OpenAPI en /openapi/v1.json (solo Development)
        app.MapOpenApi();
    }

    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // /health verifica DB (503 si Postgres no responde) — usable como healthcheck de contenedor.
    app.MapHealthChecks("/health");

    // Endpoint de ejemplo protegido: muestra el patrón [Authorize] + claims.
    app.MapGet("/api/v1/me", (ClaimsPrincipal user) => Results.Ok(new
    {
        name = user.Identity?.Name,
        roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value),
    })).RequireAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación no pudo arrancar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
