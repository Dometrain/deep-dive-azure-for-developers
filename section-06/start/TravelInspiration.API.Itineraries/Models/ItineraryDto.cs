namespace TravelInspiration.API.Itineraries.Models;

public sealed class ItineraryDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string UserId { get; set; }
}
