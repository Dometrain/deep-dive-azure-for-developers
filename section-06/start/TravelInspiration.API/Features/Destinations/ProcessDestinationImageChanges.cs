using Azure.Storage.Queues;
using MediatR;
using System.Text.Json;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public sealed class ProcessDestinationImageChanges : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/processdestinationimagechanges",
             (IMediator mediator,
                CancellationToken cancellationToken) =>
             {
                 return mediator.Send(
                     new ProcessDestinationImageChangesQuery(),
                     cancellationToken);
             }); 
    }

    public sealed class ProcessDestinationImageChangesQuery() : IRequest<IResult>
    {
    }

    public sealed class MessageDto
    {
        public required string Action { get; set; }
        public required string BlobName { get; set; } 
    }

    public sealed class ProcessDestinationImageChangesHandler(
        QueueServiceClient travelInspirationQueueServiceClient) :
       IRequestHandler<ProcessDestinationImageChangesQuery, IResult>
    {
        private readonly QueueServiceClient _travelInspirationQueueServiceClient = 
            travelInspirationQueueServiceClient;

        public async Task<IResult> Handle(ProcessDestinationImageChangesQuery request,
            CancellationToken cancellationToken)
        {
            var messageDtos = new List<MessageDto>();
            var queueClient = _travelInspirationQueueServiceClient
                .GetQueueClient("image-message-queue");

            var messages = await queueClient.ReceiveMessagesAsync(
                maxMessages: 10, 
                cancellationToken: cancellationToken);

            foreach (var message in messages.Value)
            {
                var messageTextFromQueue = JsonSerializer.Deserialize<MessageDto>(
                    message.MessageText);

                if (messageTextFromQueue != null)
                {
                    messageDtos.Add(messageTextFromQueue);
                    await queueClient.DeleteMessageAsync(
                        message.MessageId,
                        message.PopReceipt,
                        cancellationToken);
                }
            }

            return Results.Ok(messageDtos);
        }
    } 
} 