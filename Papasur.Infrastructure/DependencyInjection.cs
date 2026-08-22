using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Documentos.Ports;
using Papasur.Application.Items.Ports;
using Papasur.Application.Metrics.Ports;
using Papasur.Application.Roles.Ports;
using Papasur.Application.Statuses.Ports;
using Papasur.Application.Trazabilidad.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Infrastructure.Audit;
using Papasur.Infrastructure.Auth;
using Papasur.Infrastructure.Documentos;
using Papasur.Infrastructure.Items;
using Papasur.Infrastructure.Metrics;
using Papasur.Infrastructure.Persistence;
using Papasur.Infrastructure.Roles;
using Papasur.Infrastructure.Statuses;
using Papasur.Infrastructure.Trazabilidad;
using Papasur.Infrastructure.Users;

namespace Papasur.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("pg")
            ?? throw new InvalidOperationException("Falta la connection string 'pg' (env ConnectionStrings__pg).");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        // Configuración del JWT (sección "Jwt"): la usa JwtTokenGenerator para emitir.
        // Binding manual y LAZY (corre al resolver IOptions), así toma la key que Program.cs
        // deja resuelta en la config — la misma con la que la API valida.
        services.Configure<JwtOptions>(options =>
        {
            var section = configuration.GetSection(JwtOptions.SectionName);
            options.SymmetricKey = section["SymmetricKey"] ?? string.Empty;
            options.Issuer = section["Issuer"] ?? options.Issuer;
            options.Audience = section["Audience"] ?? options.Audience;

            if (int.TryParse(section["ExpirationMinutes"], out var minutes) && minutes > 0)
            {
                options.ExpirationMinutes = minutes;
            }
        });

        // Repositorios (implementaciones Ef* de los puertos de Application)
        services.AddScoped<IItemRepository, EfItemRepository>();
        services.AddScoped<ILoteRepository, EfLoteRepository>();
        services.AddScoped<IPlantillaRepository, EfPlantillaRepository>();
        services.AddScoped<IDocumentoRepository, EfDocumentoRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IStatusRepository, EfStatusRepository>();
        services.AddScoped<IAuditRepository, EfAuditRepository>();

        // Servicios de autenticación
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

        // Métricas: cada proveedor aporta las suyas. Agregar una métrica nueva = una línea acá.
        services.AddScoped<IMetricProvider, UserMetricProvider>();
        services.AddScoped<IMetricProvider, AuditMetricProvider>();
        services.AddScoped<IMetricProvider, ItemMetricProvider>();

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<TrazabilidadSeeder>();

        return services;
    }
}
