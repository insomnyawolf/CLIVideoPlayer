using FFMediaToolkit.Decoding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CLIVideoPlayer;

public static class Program
{
    private static async Task Main(string[] args)
    {
        FFMediaToolkit.FFmpegLoader.LoadFFmpeg();
        FFMediaToolkit.FFmpegLoader.SetupLogging();

        foreach (var file in args)
        {
            await PlayFile(file);
        }
    }

    private static async Task PlayFile(string filePath)
    {
        var file = MediaFile.Open(filePath, new MediaOptions());

        if (file.Video.Info.IsVariableFrameRate)
        {
            throw new NotImplementedException("Variable framerate videos aren't supported yet");
        }

        var framerate = file.Video.Info.AvgFrameRate;

#if DEBUG //&& !true
        framerate *= 1;
#endif

        var bufferSize = (int)framerate * 1;

        var throttleOptions = new ThrottleOptions()
        {
            MaxQueuedOperations = bufferSize,
        };

        throttleOptions.Prepare();

        var throtledFrameSource = ThrottledEnumerableSource.GetThrottledFrameSource(file.Video, throttleOptions);

        var videoSizeRaw = file.Video.GetFrame(TimeSpan.Zero).ImageSize;

        var videoSize = new Size(videoSizeRaw.Width, videoSizeRaw.Height);

        var scaledVideo = ConsoleHelpers.GetScaledToConsoleAspectRatio(videoSize);

        var consoleSafeArea = ConsoleHelpers.GetConsoleSafeArea();

        var calculatedSize = ImageHelpers.AspectRatioResizeCalculator(scaledVideo, consoleSafeArea);

        var estimatedFrameBufferSize = ImageHelpers.GetEstimatedBufferSize(calculatedSize);

        var converters = BitmapToAsciiHelper.GetBitmapToAsciiPool(estimatedFrameBufferSize);

        var render = new Render()
        {
            TargetFramerate = framerate,
        };

        var totalFrames = file.Video.Info.NumberOfFrames.Value - 1;

        var channels = new Channel<BitmapToAscii>[totalFrames];

        for (int i = 0; i < totalFrames; i++)
        {
            channels[i] = Channel.CreateBounded<BitmapToAscii>(1);
        }

        var pending = Task.Run(async () =>
        {
            // Wait for frames as soon as possible
            for (int i = 0; i < channels.Length; i++)
            {
                var converter = await channels[i].Reader.ReadAsync();

                await render.Draw(converter.FrameBuffer);

                throttleOptions.Release();

                // Return cached object
                converters.Return(converter);
            }

            Console.WriteLine("Finished");
        });

        var resizeProcessor = ImageSarpHelpers.GetResizeProcessor(videoSize, calculatedSize);

        _ = OrderedParallel.ForEachAsync(throtledFrameSource, null, (framePos, ct) =>
        {
            var image = framePos.Frame;

            image.Mutate((ob) =>
            {
                ob.ApplyProcessor(resizeProcessor);
            });

            var converter = converters.Get();

            converter.Convert(image);

            var channel = channels[framePos.Position].Writer;

            channel.TryWrite(converter);

            channel.Complete();

            return ValueTask.CompletedTask;
        });

        await pending;
    }
}
