using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace TravelInspiration.API.Itineraries;

public class GetAndCreateMostViewedItinerariesDurableFunction(
    IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    [Function(nameof(GetAndCreateMostViewedItinerariesDurableFunction))]
    public async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var hostAddress = context.GetInput<string>();

        var httpRetryOptions = new HttpRetryOptions()
        {
            MaxNumberOfAttempts = 3,
            FirstRetryInterval = TimeSpan.FromSeconds(5)
        };

        var getItinerariesResponse = await context.CallHttpAsync(
            HttpMethod.Get, 
            new Uri($"{hostAddress}/itineraries?code={_configuration["GetItinerariesFunctionKey"]}"),
            retryOptions: httpRetryOptions);

        if (getItinerariesResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return "Failed to get itineraries.";
        }

        var createMostViewedItinerariesResponse = await context.CallHttpAsync(
          HttpMethod.Post,
          new Uri($"{hostAddress}/mostvieweditineraries?code={_configuration["CreateMostViewedItinerariesFunctionKey"]}"),
          getItinerariesResponse.Content ?? "",
          retryOptions: httpRetryOptions);

        if (createMostViewedItinerariesResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return "Failed to create most viewed itineraries.";
        }

        return "Most viewed itineraries created.";
    } 

    [Function("GetAndCreateMostViewedItinerariesDurableFunction_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "generatemostvieweditineraries")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("GetAndCreateMostViewedItinerariesDurableFunction_HttpStart");

        // Get host address
        string hostAddress = $"{req.Url.Scheme}://{req.Url.Host}:{req.Url.Port}" +
            $"{req.Url.LocalPath.Substring(0, req.Url.LocalPath.IndexOf("generatemostvieweditineraries")-1)}";

        // Function input comes from the request content.
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(GetAndCreateMostViewedItinerariesDurableFunction), hostAddress);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
