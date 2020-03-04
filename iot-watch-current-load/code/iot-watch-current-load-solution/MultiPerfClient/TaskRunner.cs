using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    internal static class TaskRunner
    {
        public static IEnumerable<T[]> Segment<T>(IEnumerable<T> list, int segmentMaxLength)
        {
            var enumerator = list.GetEnumerator();
            var segment = new List<T>();

            while (enumerator.MoveNext())
            {
                do
                {
                    segment.Add(enumerator.Current);
                }
                while (segment.Count < segmentMaxLength && enumerator.MoveNext());

                yield return segment.ToArray();
                segment.Clear();
            }

            if (segment.Count > 0)
            {
                yield return segment.ToArray();
            }
        }

        public static async Task<IEnumerable<T>> RunAsync<T>(
            IEnumerable<Task<T>> tasks,
            int concurrentDegree)
        {
            var segments = Segment(tasks, concurrentDegree);
            var results = new List<T>();

            foreach (var segment in segments)
            {
                await Task.WhenAll(segment);
                results.AddRange(segment.Select(t => t.Result));
            }

            return results;
        }
    }
}