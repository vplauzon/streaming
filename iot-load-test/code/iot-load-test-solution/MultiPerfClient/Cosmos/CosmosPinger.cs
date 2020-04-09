using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Cosmos
{
    public class CosmosPinger : IDaemon
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CosmosPingerConfiguration _configuration = new CosmosPingerConfiguration();
        private readonly TelemetryClient _telemetryClient;
        private readonly Container _container;
        private IImmutableList<string> _gatewayIds = ImmutableArray<string>.Empty;

        public CosmosPinger(TelemetryClient telemetryClient)
        {
            var cosmosClient = new CosmosClient(_configuration.ConnectionString);

            _telemetryClient = telemetryClient;
            _container = cosmosClient.GetContainer("operationDb", "telemetry");
        }

        async Task IDaemon.RunAsync()
        {
            Console.WriteLine("Cosmos Pinger");

            try
            {
                Console.WriteLine("Looping for pings...");

                _gatewayIds = await LoadGatewayIdsAsync();
                await LoopMessagesAsync();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                throw;
            }
            finally
            {
                _telemetryClient.Flush();
            }
        }

        void IDaemon.Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task<IImmutableList<string>> LoadGatewayIdsAsync()
        {
            var maxTimestamp = (await LoadQueryAsync<long>(
                "SELECT VALUE MAX(c._ts) FROM c")).First();
            //  Go back 60 seconds to look for all gateways present then
            var gatewaysIds = await LoadQueryAsync<string>(
                new QueryDefinition("SELECT DISTINCT VALUE c.gatewayId FROM c WHERE c._ts > @minTimestamp")
                .WithParameter("@minTimestamp", maxTimestamp - 60));

            return gatewaysIds.ToImmutableArray();
        }

        private async Task LoopMessagesAsync()
        {
            await Task.Delay(3);
            //var context = new MessageLoopContext(_configuration, MESSAGE_TIMEOUT);

            //await context.LoopMessagesAsync(
            //    gateways,
            //    _telemetryClient,
            //    _cancellationTokenSource.Token);
        }

        private async Task<IEnumerable<T>> LoadQueryAsync<T>(string query)
        {
            return await LoadQueryAsync<T>(new QueryDefinition(query));
        }

        private async Task<IEnumerable<T>> LoadQueryAsync<T>(QueryDefinition queryDefinition)
        {
            var iterator = _container.GetItemQueryIterator<T>(queryDefinition);

            return await LoadQueryAsync(iterator);
        }

        private async Task<IEnumerable<T>> LoadQueryAsync<T>(FeedIterator<T> iterator)
        {
            var list = new List<T>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();

                list.AddRange(response);
            }

            return list;
        }
    }
}