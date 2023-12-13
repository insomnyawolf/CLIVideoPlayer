using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CLIVideoPlayer;

// https://stackoverflow.com/questions/3705006/is-it-possible-to-change-paralleloptions-maxdegreeofparallelism-during-execution
internal class DynamicParallel
{
    /// <summary>
    /// Executes a parallel for-each operation on an async-enumerable sequence,
    /// enforcing a dynamic maximum degree of parallelism.
    /// </summary>
    public static Task ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        DynamicParallelOptions options,
        Func<TSource, CancellationToken, ValueTask> body)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (body == null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        var throttler = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        options.DegreeOfParallelismChangedDelta += Options_ChangedDelta;
        void Options_ChangedDelta(object sender, int delta)
        {
            if (delta > 0)
            {
                throttler.Release(delta);
            }
            else
            {
                for (int i = delta; i < 0; i++)
                {
                    throttler.WaitAsync();
                }
            }
        }

        IEnumerable<TSource> GetThrottledSource()
        {
            foreach (var item in source)
            {
                throttler.Wait();
                yield return item;
            }
        }

        return Parallel.ForEachAsync(GetThrottledSource(), options, async (item, ct) =>
        {
            try { await body(item, ct).ConfigureAwait(false); }
            finally { throttler.Release(); }
        }).ContinueWith(t =>
        {
            options.DegreeOfParallelismChangedDelta -= Options_ChangedDelta;
            return t;
        }, default, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default)
            .Unwrap();
    }
}

/// <summary>
/// Stores options that configure the DynamicParallelForEachAsync method.
/// </summary>
public class DynamicParallelOptions : ParallelOptions
{
    private int _maxDegreeOfParallelism;

    public event EventHandler<int> DegreeOfParallelismChangedDelta;

    public DynamicParallelOptions()
    {
        // Set the base DOP to the maximum.
        // That's what the native Parallel.ForEachAsync will see.
        base.MaxDegreeOfParallelism = Int32.MaxValue;
        _maxDegreeOfParallelism = Environment.ProcessorCount;
    }

    public new int MaxDegreeOfParallelism
    {
        get { return _maxDegreeOfParallelism; }
        set
        {
            //// commenting that let's us stop when we don't need more data
            //if (value < 1) throw new ArgumentOutOfRangeException();
            if (value == _maxDegreeOfParallelism)
            {
                return;
            }

            int delta = value - _maxDegreeOfParallelism;
            DegreeOfParallelismChangedDelta?.Invoke(this, delta);
            _maxDegreeOfParallelism = value;
        }
    }
}
