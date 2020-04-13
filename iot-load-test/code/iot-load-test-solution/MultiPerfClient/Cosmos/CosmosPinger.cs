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

                await LoadGatewayIdsAsync();
                await LoopPingAsync();
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

        private async Task LoadGatewayIdsAsync()
        {
            var maxTimestamp = (await LoadQueryAsync<long>(
                "SELECT VALUE MAX(c._ts) FROM c")).First();
            //  Go back 60 seconds to look for all gateways present then
            var gatewaysIds = await LoadQueryAsync<string>(
                new QueryDefinition("SELECT DISTINCT VALUE c.gatewayId FROM c WHERE c._ts > @minTimestamp")
                .WithParameter("@minTimestamp", maxTimestamp - 60));

            _gatewayIds = gatewaysIds.ToImmutableArray();
        }

        private async Task LoopPingAsync()
        {
            var pingTasks = from i in Enumerable.Range(0, _configuration.ConcurrentCallCount)
                            select PeriodicPingCosmosAsync();
            var loadGatewayTask = PeriodicLoadGatewayIdsAsync();

            await Task.WhenAll(pingTasks.Prepend(loadGatewayTask));
        }

        private async Task PeriodicLoadGatewayIdsAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                await LoadGatewayIdsAsync();
            }
        }

        private async Task PeriodicPingCosmosAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await PingCosmosAsync();
            }
        }

        private Task PingCosmosAsync()
        {
            throw new NotImplementedException();
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