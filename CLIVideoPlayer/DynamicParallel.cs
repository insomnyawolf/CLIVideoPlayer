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
    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, DynamicParallelOptions options, Func<TSource, CancellationToken, ValueTask> body)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(options);

        ArgumentNullException.ThrowIfNull(body);

        options.Prepare();

        return Parallel.ForEachAsync(GetThrottledSource(source, options), options, async (item, ct) =>
        {
            await body(item, ct).ConfigureAwait(false);
        });
    }

    private static IEnumerable<TSource> GetThrottledSource<TSource>(IEnumerable<TSource> source, DynamicParallelOptions options)
    {
        foreach (var item in source)
        {
            options.WaitAvailable();
            yield return item;
        }
    }
}

/// <summary>
/// Stores options that configure the DynamicParallelForEachAsync method.
/// </summary>
public class DynamicParallelOptions : ParallelOptions
{
    public int MaxParalelismTarget { get; set; }
    internal CustomSemaphoreSlim Semaphore;

    internal void Prepare()
    {
        Semaphore = new CustomSemaphoreSlim(MaxParalelismTarget);
    }

    internal void WaitAvailable()
    {
        Semaphore.Wait();
    }

    internal void Release()
    {
        Semaphore.Release();
    }
}
