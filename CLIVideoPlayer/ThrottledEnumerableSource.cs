using FFMediaToolkit.Decoding;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Collections.Generic;

namespace CLIVideoPlayer;

// https://stackoverflow.com/questions/3705006/is-it-possible-to-change-paralleloptions-maxdegreeofparallelism-during-execution
internal class ThrottledEnumerableSource
{
    // This exists to avoid processing the whole video in one go
    // Because that could cause a laggy startup and very high memory usage
    public static int ProcessedFrames = 0;
    public static IEnumerable<FramePosition> GetThrottledFrameSource(VideoStream video, ThrottleOptions options)
    {
        while (video.TryGetNextFrame(out var frame))
        {
            var res = new FramePosition()
            {
                Frame = ImageSarpHelpers.ToImage(frame),
                Position = ProcessedFrames,
            };

            ProcessedFrames++;

            options.WaitAvailable();
            yield return res;
        }
    }
}

public class FramePosition
{
    public int Position { get; set; }
    public Image<Bgr24> Frame { get; set; }
}

public class ThrottleOptions
{
#warning would be cool if i could change that on the fly
    public int MaxQueuedOperations { get; set; }
    internal CustomSemaphoreSlim Semaphore;

    internal void Prepare()
    {
        Semaphore = new CustomSemaphoreSlim(MaxQueuedOperations);
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
