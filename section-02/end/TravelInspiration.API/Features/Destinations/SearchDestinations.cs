using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public sealed class SearchDestinations : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/destinations",
             (string? searchFor,
                IMediator mediator,
                CancellationToken cancellationToken) =>
             {
                 return mediator.Send(
                     new SearchDestinationsQuery(searchFor),
                     cancellationToken);
             });
    }

    public sealed class SearchDestinationsQuery(string? searchFor) : IRequest<IResult>
    {
        public string? SearchFor { get; } = searchFor;
    }

    public sealed class DestinationDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public List<string> ImageUris { get; set; } = [];

    }

    public sealed class SearchDestinationsHandler(IConfiguration configuration,
        TableServiceClient travelInspirationTableServiceClient,
        BlobServiceClient travelInspirationBlobServiceClient) :
       IRequestHandler<SearchDestinationsQuery, IResult>
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly TableServiceClient _travelInspirationTableServiceClient = travelInspirationTableServiceClient;
        private readonly BlobServiceClient _travelInspirationBlobServiceClient = travelInspirationBlobServiceClient;

        public Task<IResult> Handle(SearchDestinationsQuery request,
            CancellationToken cancellationToken)
        { 
            var destinationsTableClient = _travelInspirationTableServiceClient
                .GetTableClient("Destinations");

            var filter = request.SearchFor == null ? "" :
                TableClient.CreateQueryFilter($"Name eq {request.SearchFor}");
            var amountToReturn = _configuration.GetValue<int>("Destinations:AmountToReturn");

            var destinations = destinationsTableClient.Query<TableEntity>(filter,
                amountToReturn,
                ["Identifier", "Name"],
                cancellationToken);

            var pagedDestinationDtos = destinations.AsPages()
                .First().Values.Select(d => new DestinationDto
                { 
                    Id = d.GetInt32("Identifier") ?? -1,
                    Name = d.GetString("Name"),
                }).ToList();

            if (pagedDestinationDtos.Count == 0)
            {
                return Task.FromResult(Results.Ok());
            }

            var destinationImagesContainerClient = _travelInspirationBlobServiceClient
                .GetBlobContainerClient("destination-images");

            foreach (var destination in pagedDestinationDtos)
            {
                var blobs = destinationImagesContainerClient.
                    FindBlobsByTags($"DestinationIdentifier='{destination.Id}'",
                    cancellationToken);

                foreach (var blob in blobs)
                {
                    var blobClient = destinationImagesContainerClient
                        .GetBlobClient(blob.BlobName);
                    destination.ImageUris.Add(blobClient.Uri.AbsoluteUri);
                }
            }
            
            // return results, returning only the amount described in settings
            return Task.FromResult(Results.Ok(pagedDestinationDtos));
        }
    }

}
