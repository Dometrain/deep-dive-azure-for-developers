using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TravelInspiration.API.Shared.Behaviours;
using TravelInspiration.API.Shared.Metrics;
using TravelInspiration.API.Shared.Persistence;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    { 
        services.RegisterSlices();

        var currentAssembly = Assembly.GetExecutingAssembly();
        services.AddAutoMapper(currentAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(currentAssembly)
            .RegisterServicesFromAssemblies(currentAssembly)
                .AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>))
                .AddOpenBehavior(typeof(ModelValidationBehaviour<,>))
                .AddOpenBehavior(typeof(HandlerPerformanceMetricBehaviour<,>));
        }); 
        services.AddValidatorsFromAssembly(currentAssembly);
        services.AddSingleton<HandlerPerformanceMetric>(); 
        return services;
    }

    public static IServiceCollection RegisterPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var credential = new DefaultAzureCredential();

        // Get a token to access Azure SQL
        var accessTokenResponse = credential.GetToken(
            new Azure.Core.TokenRequestContext(["https://database.windows.net/.default"]));

        var sqlConnection = new SqlConnection(
            configuration.GetConnectionString("TravelInspirationDbConnection"))
        {
            AccessToken = accessTokenResponse.Token
        };

        services.AddDbContext<TravelInspirationDbContext>(options =>
            options.UseSqlServer(sqlConnection,
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services.AddScoped(sp =>
        {
            return new TableServiceClient(
                configuration.GetConnectionString("TravelInspirationStorageConnectionString"));
        });

        services.AddScoped(sp =>
        {
            return new BlobServiceClient(
                configuration.GetConnectionString("TravelInspirationStorageConnectionString"));
        });

        services.AddScoped(sp =>
        {
            return new QueueServiceClient(
                configuration.GetConnectionString("TravelInspirationStorageConnectionString"));
        });

        return services;
    }
}
