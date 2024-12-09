using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using TravelInspiration.API.Itineraries.Models;

namespace TravelInspiration.API.Itineraries;

public class CreateMostViewedItinerariesFunction
{
    private readonly ILogger<CreateMostViewedItinerariesFunction> _logger;

    public CreateMostViewedItinerariesFunction(ILogger<CreateMostViewedItinerariesFunction> logger)
    {
        _logger = logger;
    }

    [Function("CreateMostViewedItinerariesFunction")]
    [OpenApiOperation("CreateMostViewedItinerariesFunction",
        "CreateMostViewedItinerariesFunction",
        Description = "Create a list of most-viewed itineraries itineraries")]
    [OpenApiRequestBody("application/json",
        typeof(List<ItineraryDto>),
        Description = "List of itineraries to create most viewed itineraries for the current user.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK,
        "application/json",
        typeof(string))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", 
        Route = "mostvieweditineraries")] HttpRequest req)
    {
        // Read request body 
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var itineraries = JsonSerializer.Deserialize<List<ItineraryDto>>(requestBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // do something with the itineraries
         
        return new OkObjectResult("Most viewed itineraries have been creatd for the current user.");
    }
}
