namespace TravelInspiration.API.Shared.Domain.Models;

public class Destination(string name)  
{
    public required int Id { get; set; }
    public required string Name { get; set; } = name; 
    public string? ImageName { get; set; }
 
}