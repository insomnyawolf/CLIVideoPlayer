﻿using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using Microsoft.Extensions.ObjectPool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CLIVideoPlayer;

public static class Program
{
    private static async Task Main(string[] args)
    {
        //var exeLocation = AppDomain.CurrentDomain.BaseDirectory;
        //FFMediaToolkit.FFmpegLoader.FFmpegPath = Path.Combine(Path.GetDirectoryName(exeLocation), "ffmpeg");
        FFMediaToolkit.FFmpegLoader.LoadFFmpeg();
        FFMediaToolkit.FFmpegLoader.SetupLogging();

        //foreach (var file in args)
        //{
        //    using var output  = new FileStream(file + ".txt", FileMode.CreateNew, FileAccess.Write);
        //    await ConvertPictureAsync(file, output);
        //    await output.FlushAsync();
        //}

        //return;

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

        // edit this if the image is too small or too big and makes earthquakes
        const int safeArea = 1;

        var consoleSize = new Size(Console.WindowWidth - safeArea, Console.WindowHeight - safeArea);

        var videoSizeRaw = file.Video.GetFrame(TimeSpan.Zero).ImageSize;

        var videoSize = new Size(videoSizeRaw.Width, videoSizeRaw.Height);

        videoSize.Width *= 2;

        var calculatedSize = AspectRatioResizeCalculator(videoSize, consoleSize);

        var converters = new DefaultObjectPoolProvider().Create(new BitmapToAsciiPooledObjectPolicy()
        {
            CacheDefaultCapacity = 0,
        });

        var render = new Render()
        {
            TargetFramerate = framerate,
        };

        var totalFrames = file.Video.Info.NumberOfFrames.Value;

        var channels = new List<Channel<BitmapToAscii>>(totalFrames);

        for (int i = 0; i < totalFrames; i++)
        {
            channels.Add(Channel.CreateBounded<BitmapToAscii>(1));
        }

        var bufferSize = (int)framerate * 1;

        var parallelOptions = new DynamicParallelOptions()
        {
            // Oh wait wtf, it's so optimized that can run test on single core at 35fps on hd when unlimited D:
            // but if you add more threads it gets crazy with the memory allocations and has a unstable framerate when preloading the video
#if DEBUG && !true
            MaxDegreeOfParallelism = 1,
#endif
            MaxParalelismTarget = bufferSize,
        };

        var pending = Task.Run(async () =>
        {
            // Wait for frames as soon as possible
            for (int i = 0; i < channels.Count; i++)
            {
                // Paralelization limit to avoid pre-loading the whole video at once
                // we only cache n seconds
                var preRenderFrameObjetive = i + bufferSize;

                // we compare the started process to the objetive
                int objetiveDifference = preRenderFrameObjetive - StartedToProcessProcessedFrames;

                var converter = await channels[i].Reader.ReadAsync();

                await render.Draw(converter.FrameBuffer);

                parallelOptions.Release();

                // Return cached object
                converters.Return(converter);
            }
        });

        await DynamicParallel.ForEachAsync(file.Video.GetFramesEnumerable(), parallelOptions, async (framePos, ct) =>
        {
            var image = framePos.Frame;

            image.Mutate((ob) =>
            {
                ob.Resize(calculatedSize);
            });

            var converter = converters.Get();

            converter.Convert(image);

            var channel = channels[framePos.Position].Writer;

            channel.TryWrite(converter);

            channel.Complete();
        });

        await pending;
    }

    public static int StartedToProcessProcessedFrames = 0;

    private static readonly Configuration Configuration = new()
    {
        PreferContiguousImageBuffers = true,
    };

    public static Image<Bgr24> ToBitmap(this ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(Configuration, imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }

    private static IEnumerable<FramePosition> GetFramesEnumerable(this VideoStream video)
    {
        while (video.TryGetNextFrame(out var frame))
        {
            var res = new FramePosition()
            {
                Frame = frame.ToBitmap(),
                Position = StartedToProcessProcessedFrames,
            };

            StartedToProcessProcessedFrames++;

            yield return res;
        }
    }

    public class FramePosition
    {
        public int Position { get; set; }
        public Image<Bgr24> Frame { get; set; }
    }

    public static Size AspectRatioResizeCalculator(Size origin, Size target)
    {
        var width = origin.Width;
        var height = origin.Height;

        decimal coefficientFitWidth = CoefficientChange(width, target.Width);
        decimal coefficientFitHeight = CoefficientChange(height, target.Height);

        decimal coefficient = coefficientFitWidth < coefficientFitHeight ? coefficientFitWidth : coefficientFitHeight;

        // Avoid Upscaling
        if (coefficient > 1)
        {
            return origin;
        }

        width = decimal.ToInt32(coefficient * origin.Width);
        height = decimal.ToInt32(coefficient * origin.Height);

        // Images must have at least 1 px on both sides
        // This fixes it
        coefficient = 0;
        if (width < 1)
        {
            coefficient = CoefficientChange(width, 1);
        }
        else if (height < 1)
        {
            coefficient = CoefficientChange(height, 1);
        }

        if (coefficient != 0)
        {
            height = decimal.ToInt32(coefficient * origin.Width);
            width = decimal.ToInt32(coefficient * origin.Height);
        }

        return new Size
        {
            Width = width,
            Height = height,
        };
    }

    private static decimal CoefficientChange(int valorInicial, int valorFinal)
    {
        return 100M / valorInicial * valorFinal / 100;
    }























    public static async Task ConvertPictureAsync(string path, Stream output)
    {
        var image = await Image.LoadAsync(path);

        var targetSize = new Size(1080, 1920) / 10;

        var resize = AspectRatioResizeCalculator(image.Size, targetSize);

        resize.Width *= 2;

        image.Mutate((ob) =>
        {
            ob.Resize(resize);
        });

        var converter = new BitmapToAscii(new MemoryStream(0));

        var img = image.CloneAs<Bgr24>();

        converter.Convert(img);

        await converter.FrameBuffer.CopyToAsync(output);
    }
}
