using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using TravelInspiration.API.Itineraries.DbContexts;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((appBuilder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var credential = new DefaultAzureCredential();

        // Get a token to access Azure SQL
        var accessTokenResponse = credential.GetToken(
            new Azure.Core.TokenRequestContext(["https://database.windows.net/.default"]));

        var sqlConnection = new SqlConnection(
            appBuilder.Configuration.GetConnectionString("TravelInspirationDbConnection"))
        {
            AccessToken = accessTokenResponse.Token
        };

        services.AddDbContext<TravelInspirationDbContext>(options =>
            options.UseSqlServer(sqlConnection,
                sqlOptions => sqlOptions.EnableRetryOnFailure()));
    })
    .Build();

host.Run();
