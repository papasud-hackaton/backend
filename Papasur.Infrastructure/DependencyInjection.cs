using Papasur.Application.Items.Ports;
using Papasur.Infrastructure.Items;
using Papasur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Repositorios (implementaciones Ef* de los puertos de Application)
        services.AddScoped<IItemRepository, EfItemRepository>();

        return services;
    }
}
