using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            FFMediaToolkit.FFmpegLoader.FFmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");

            ConsoleHelper.PrepareConsole(3);

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

            var framePeriod = TimeSpan.FromMilliseconds(1000 / (framerate * 1.5));

            var size = new Size(Console.WindowWidth, Console.WindowHeight - 3);

            size = BulkImageResizer.AspectRatioResizeCalculator(file.Video.Info.FrameSize, size);

            // This fixed the console characters not being 1:1
            //size.Width *= 2;

            var bitmapToAscii = new BitmapToAscii();

            var temp = file.Video.GetFrame(TimeSpan.Zero).ToBitmap();

            var bulkResize = new BulkImageResizer(size, temp.HorizontalResolution, temp.VerticalResolution);

            try
            {
                while (true)
                {
                    // I don't use TryGetNextFrame because internally does the same and doesn't let me use async code
                    var raw = file.Video.GetNextFrame().ToBitmap();

                    var resized = bulkResize.Resize(raw);

                    var textImg = bitmapToAscii.GrayscaleImageToASCIIBasic(resized);

                    Render.NextFrame(textImg);

                    //await Task.Delay(framePeriod);
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
