using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CLIVideoPlayer;

internal class OrderedParallel
{
    private static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions()
    {
        // Oh wait wtf, it's so optimized that can run test on single core at 35fps on hd when unlimited D:
        // but if you add more threads it gets crazy with the memory allocations and has a unstable framerate when preloading the video
#if DEBUG //&& !true
            MaxDegreeOfParallelism = 1,
#endif
    };

    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions options, Func<TSource, CancellationToken, ValueTask> body)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(body);

        options ??= DefaultParallelOptions;

        return Parallel.ForEachAsync(source, options, body);
    }
}
