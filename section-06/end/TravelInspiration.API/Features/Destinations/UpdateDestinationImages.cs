using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using MediatR;
using System.Text;
using System.Text.Json;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public class UpdateDestinationImages : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPut("api/destinations/{destinationId}/images",
            (int destinationId,
            UpdateDestinationImagesCommand updateDestinationImagesCommand,
            IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                // make sure the destinationId from the Uri is used
                updateDestinationImagesCommand.DestinationId = destinationId;
                return mediator.Send(
                  updateDestinationImagesCommand,
                  cancellationToken);
            });
    }

    public sealed class UpdateDestinationImagesCommand : IRequest<IResult>
    {
        public sealed class ImageToUpdate
        {
            public required string Name { get; set; }
            public required string ImageBytes { get; set; }
        }
        public int DestinationId { get; set; }
        public List<ImageToUpdate> ImagesToUpdate { get; set; } = [];
    }


    public sealed class UpdateDestinationImagesCommandHandler(
        IConfiguration configuration,
        BlobServiceClient travelInspirationBlobServiceClient,
        EventGridPublisherClient eventGridPublisherClient) :
        IRequestHandler<UpdateDestinationImagesCommand, IResult>
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly BlobServiceClient _travelInspirationBlobServiceClient = travelInspirationBlobServiceClient;
        private readonly EventGridPublisherClient _eventGridPublisherClient = eventGridPublisherClient;

        public async Task<IResult> Handle(UpdateDestinationImagesCommand request,
            CancellationToken cancellationToken)
        {
            var destinationImagesContainerClient = 
                _travelInspirationBlobServiceClient
                    .GetBlobContainerClient("destination-images");

            foreach (var imageToUpload in request.ImagesToUpdate)
            {
                // get the blob 
                var blobClient = destinationImagesContainerClient
                    .GetBlobClient(imageToUpload.Name);

                if (!blobClient.Exists(cancellationToken).Value)
                {
                    using (var stream = new MemoryStream(
                        Encoding.UTF8.GetBytes(imageToUpload.ImageBytes)))
                    {
                        await blobClient.UploadAsync(stream,
                            new BlobUploadOptions()
                            {
                                Tags = new Dictionary<string, string>
                                    { { "DestinationIdentifier",
                                        request.DestinationId.ToString() } }
                            },
                            cancellationToken);
                    } 
                }
                else
                {
                    // get the tags 
                    var blobTags = blobClient.GetTags(cancellationToken: cancellationToken);

                    // check tag 
                    if (blobTags.Value.Tags.TryGetValue("DestinationIdentifier",
                            out var destinationId) && destinationId == request.DestinationId.ToString())
                    {
                        using (var stream = new MemoryStream(
                             Encoding.UTF8.GetBytes(imageToUpload.ImageBytes)))
                        {
                            await blobClient.UploadAsync(stream,
                                new BlobUploadOptions()
                                {
                                    Tags = new Dictionary<string, string>
                                        { { "DestinationIdentifier",
                                        request.DestinationId.ToString() } }
                                },
                                cancellationToken);
                        } 
                    }
                    else
                    {
                        // log, throw, ...
                    }
                }

                var imageCloudEvent = new CloudEvent(
                    $"destinations/{request.DestinationId}/images/{imageToUpload.Name}",
                    "com.ourcompany.destination-image-updated-or-created",
                    new { BlobName = imageToUpload.Name, DestinationId = request.DestinationId })
                {
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTimeOffset.UtcNow
                };

                await _eventGridPublisherClient.SendEventAsync(imageCloudEvent);
            } 

            return Results.Ok();
        }
    }
}
