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

            //ConsoleHelper.PrepareConsole(2);
            ConsoleHelper.PrepareConsole(3);
            //ConsoleHelper.PrepareConsole(6);
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

            if (file.Video.Info.IsVariableFrameRate)
            {
                throw new NotImplementedException("Variable framerate videos aren't supported yet");
            }

            var framerate = file.Video.Info.AvgFrameRate;

            // Render.Title($"ShellEngine Test, Playing {Path.GetFileName(filePath)} at {framerate} fps");

            double framePeriod = 1000 / framerate;

            var size = new Size(Console.WindowWidth, Console.WindowHeight - 3);

            size = BulkImageResizer.AspectRatioResizeCalculator(file.Video.Info.FrameSize, size);

            // This fixed the console characters not being 1:1
            // Works for linux, on window sit's replaced by ConsoleHelper.PrepareConsole()
            //size.Width *= 2;

            var bitmapToAscii = new BitmapToAscii();

            var temp = file.Video.GetFrame(TimeSpan.Zero).ToBitmap();

            var bulkResize = new BulkImageResizer(size, temp.HorizontalResolution, temp.VerticalResolution);

            var watch = Stopwatch.StartNew();

            long frameDecodingDelay;
            long frameRenderDelay = 0;
            int frameDelay;

            try
            {
                for (int i = 0; i < file.Video.Info.NumberOfFrames.Value; i++)
                {
                   // I don't use TryGetNextFrame because internally does the same and doesn't let me use async code

                   var raw = file.Video.GetNextFrame().ToBitmap();

                    watch.Restart();

                    var resized = bulkResize.Resize(raw);

                    var textImg = bitmapToAscii.GetString(resized);

                    frameDecodingDelay = watch.ElapsedMilliseconds;

                    frameDelay = (int)(framePeriod - frameDecodingDelay - frameRenderDelay);

                    if (frameDelay < 0)
                    {
                        frameDelay = 0;
                    }

                    await Task.Delay(frameDelay);

                    watch.Restart();

                    // I don't understand why or how this works, but it does (maybe)
                    // It causes weird glitches when render times is higer than decoding delay

                    Task.Run(() => Render.NextFrame(textImg));

                    frameRenderDelay = watch.ElapsedMilliseconds;
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
