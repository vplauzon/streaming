using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    internal static class TaskRunner
    {
        #region Inner Types
        public class TempoRunResult
        {
            public TempoRunResult(int iterationCount, int pauseCount)
            {
                IterationCount = iterationCount;
                PauseCount = pauseCount;
            }

            public int IterationCount { get; }

            public int PauseCount { get; }
        }

        public class TempoRunResult<T> : TempoRunResult
        {
            public TempoRunResult(int iterationCount, int pauseCount, IEnumerable<T> values)
                : base(iterationCount, pauseCount)
            {
                Values = values.ToImmutableArray();
            }

            public IImmutableList<T> Values { get; }
        }
        #endregion

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

        public static async Task<TempoRunResult> TempoRunAsync(
            IEnumerable<Task> tasks,
            TimeSpan durationPerTask,
            CancellationToken token)
        {
            Func<Task, Task<int>> wrap = async (t) =>
            {
                await t;

                return 0;
            };
            var stronglyTypedTasks = from t in tasks
                                     select wrap(t);

            return await TempoRunAsync<int>(stronglyTypedTasks, durationPerTask, token);
        }

        public static async Task<TempoRunResult<T>> TempoRunAsync<T>(
            IEnumerable<Task<T>> tasks,
            TimeSpan durationPerTask,
            CancellationToken token)
        {
            var results = new List<T>();
            var iterationCount = 0;
            var pauseCount = 0;

            foreach (var t in tasks)
            {
                var watch = new Stopwatch();

                watch.Start();

                results.Add(await t);

                var requiredPause = durationPerTask - watch.Elapsed;

                if (requiredPause > TimeSpan.Zero)
                {
                    await Task.Delay(requiredPause, token);
                    ++pauseCount;
                }
                ++iterationCount;
            }

            return new TempoRunResult<T>(iterationCount, pauseCount, results);
        }
    }
}