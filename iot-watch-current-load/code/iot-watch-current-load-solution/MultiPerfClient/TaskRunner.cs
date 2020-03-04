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
            var windowTasks = new List<Task<T>>();
            var results = new List<T>();

            while (enumerator.MoveNext())
            {
                do
                {
                    windowTasks.Add(enumerator.Current);
                }
                while (windowTasks.Count < concurrentDegree && enumerator.MoveNext());

                await Task.WhenAll(windowTasks);
                results.AddRange(windowTasks.Select(t => t.Result));
            }

            return results;
        }
    }
}