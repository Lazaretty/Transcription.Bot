using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transcription.DAL.Repositories;

namespace Transcription.DAL;

public static class WBDALExtensions
{
    public static IServiceCollection AddDALServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(nameof(WbContext)));
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<UserRepository>();
        services.AddScoped<ChatStateRepository>();
        services.AddScoped<YandexRequestRepository>();
        
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        return services;
    }
}