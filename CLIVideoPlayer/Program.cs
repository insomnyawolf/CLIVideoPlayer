using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            var exeLocation = Assembly.GetEntryAssembly().Location;
            FFMediaToolkit.FFmpegLoader.FFmpegPath = Path.Combine(Path.GetDirectoryName(exeLocation), "ffmpeg");

            //ConsoleHelper.PrepareConsole(3);
            ConsoleHelper.PrepareConsole(6);
            //ConsoleHelper.PrepareConsole(13);

            foreach (var file in args)
            {
                await PlayFile(file);
            }

            ConsoleHelper.RestoreConsole();
        }

        private static async Task PlayFile(string filePath)
        {
            var file = MediaFile.Open(filePath);

            var framerate = file.Video.Info.AvgFrameRate;

            Render.Title($"ShellEngine Test, Playing {Path.GetFileName(filePath)} at {framerate} fps");

            var framePeriod = TimeSpan.FromMilliseconds(1000 / framerate);

            var size = new Size(Console.WindowWidth, Console.WindowHeight - 3);

            size = BulkImageResizer.AspectRatioResizeCalculator(file.Video.Info.FrameSize, size);

            // This fixed the console characters not being 1:1
            //size.Width *= 2;

            var bitmapToAscii = new BitmapToAscii();

            var temp = file.Video.GetFrame(TimeSpan.Zero).ToBitmap();

            var bulkResize = new BulkImageResizer(size, temp.HorizontalResolution, temp.VerticalResolution);

            var watch = Stopwatch.StartNew();

            TimeSpan frameRenderDelay;

            try
            {
                while (true)
                {
                    // I don't use TryGetNextFrame because internally does the same and doesn't let me use async code
                    watch.Restart();

                    var raw = file.Video.GetNextFrame().ToBitmap();

                    var resized = bulkResize.Resize(raw);

                    var textImg = bitmapToAscii.GrayscaleImageToASCIIBasic(resized);

                    Render.NextFrame(textImg);

                    frameRenderDelay = framePeriod - TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);

                    if (frameRenderDelay < TimeSpan.Zero)
                    {
                        frameRenderDelay = TimeSpan.Zero;
                    }

                    await Task.Delay(frameRenderDelay);
                }
            }
            catch (EndOfStreamException)
            {
            }
        }

        private static unsafe Bitmap ToBitmap(this ImageData bitmap)
        {
            fixed (byte* p = bitmap.Data)
            {
                return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(p));
            }
        }
    }
}
