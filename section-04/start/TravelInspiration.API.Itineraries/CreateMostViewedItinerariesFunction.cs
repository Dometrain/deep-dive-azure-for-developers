using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", 
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
