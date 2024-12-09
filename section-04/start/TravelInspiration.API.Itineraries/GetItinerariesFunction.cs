using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelInspiration.API.Itineraries.DbContexts;
using TravelInspiration.API.Itineraries.Models;

namespace TravelInspiration.API.Itineraries;

public class GetItinerariesFunction(ILogger<GetItinerariesFunction> logger,
    TravelInspirationDbContext dbContext,
    IConfiguration configuration)
{
    private readonly ILogger<GetItinerariesFunction> _logger = logger;
    private readonly TravelInspirationDbContext _dbContext = dbContext;
    private readonly IConfiguration _configuration = configuration;

    [Function("GetItinerariesFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "itineraries")] 
        HttpRequest req)
    {
        string? searchForValue = req.Query["SearchFor"];

        if (!int.TryParse(_configuration["MaximumAmountOfItinerariesToReturn"],
            out int maximumAmountOfItinerariesToReturn))
        {
            throw new Exception("MaximumAmountOfItinerariesToReturn setting is missing or its value is not a valid integer.");
        }


        var itineraryEntities = await _dbContext.Itineraries
            .Where(i => searchForValue == null ||
                           i.Name.Contains(searchForValue) ||
                           (i.Description != null && i.Description.Contains(searchForValue)))
            .OrderBy(i => i.Name)
            .Take(maximumAmountOfItinerariesToReturn)
            .ToListAsync();

        var itineraryDtos = itineraryEntities.Select(i => new ItineraryDto()
        {
            Id = i.Id,
            Description = i.Description,
            Name = i.Name,
            UserId = i.UserId
        });


        return new OkObjectResult(itineraryDtos);
    }
}