using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    internal static class TaskRunner
    {
        public static async Task<IEnumerable<T>> RunAsync<T>(
            IEnumerable<Task<T>> tasks,
            int concurrentDegree)
        {
            var enumerator = tasks.GetEnumerator();
            var windowTasks = new Queue<Task<T>>();
            var results = new List<T>();

            while (enumerator.MoveNext())
            {
                windowTasks.Enqueue(enumerator.Current);
                if (windowTasks.Count >= concurrentDegree)
                {
                    var nextReady = windowTasks.Dequeue();
                    var nextResult = await nextReady;

                    results.Add(nextResult);
                }
            }
            await Task.WhenAll(windowTasks);
            results.AddRange(windowTasks.Select(t => t.Result));

            return results;
        }
    }
}