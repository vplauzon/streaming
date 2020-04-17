using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    public static class CosmosHelper
    {
        public static async Task<IImmutableList<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            var list = new List<T>();

            await foreach (var item in enumerable)
            {
                list.Add(item);
            }

            return list.ToImmutableArray();
        }

        public static async Task<IImmutableList<T>> ToListAsync<T>(this FeedIterator<T> feedIterator)
        {
            var list = new List<T>();

            while(feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();

                list.AddRange(response);
            }

            return list.ToImmutableArray();
        }
    }
}