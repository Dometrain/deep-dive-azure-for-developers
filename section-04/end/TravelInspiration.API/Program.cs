using TravelInspiration.API;
using TravelInspiration.API.Shared.Slices;
using Microsoft.Extensions.Azure;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor(); 
   
builder.Services.RegisterApplicationServices();
builder.Services.RegisterPersistenceServices(builder.Configuration);
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnectionString:blobServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnectionString");
    clientBuilder.AddQueueServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnectionString:queueServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnectionString");
    clientBuilder.AddTableServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnectionString:tableServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnectionString");
});
builder.Services.Configure<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(config =>
{
config.SetAzureTokenCredential(new DefaultAzureCredential());
});

builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();
 
app.MapSliceEndpoints();

app.Run();