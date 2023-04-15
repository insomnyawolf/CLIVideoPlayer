using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using Microsoft.Extensions.ObjectPool;
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
            //ConsoleHelper.PrepareConsole(9);
            //ConsoleHelper.PrepareConsole(13);

            foreach (var file in args)
            {
                //Render.StdOut = new FileStream(file + ".txt", FileMode.CreateNew, FileAccess.Write);
                await PlayFile(file);
                //Render.StdOut.Flush();
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

            double framePeriod = 1000 / framerate;

            // edit this if the image is too small or too big and makes earthquakes
            int safeArea = 1;

            var Size = new Size(Console.WindowWidth - safeArea, Console.WindowHeight - safeArea);

            var temp = file.Video.GetFrame(TimeSpan.Zero).ToBitmap();

            Size = BulkImageResizer.AspectRatioResizeCalculator(temp.Size, Size);

            var resizers = new DefaultObjectPoolProvider().Create(new ImageConverterPooledObjectPolicy()
            {
                BulkImageResizerSettings = new BulkImageResizerSettings()
                {
                    Size = Size,
                    HorizontalResolution = temp.HorizontalResolution,
                    VerticalResolution = temp.VerticalResolution,
                    ResizerQuality = ResizerQuality.HighSpeed,
                }
            });

            var render = new Render(Size);

            var watch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < file.Video.Info.NumberOfFrames.Value; i++)
                {
                    // I don't use TryGetNextFrame because internally does the same and doesn't let me use async code
                    var raw = file.Video.GetNextFrame().ToBitmap();

                    // Get Cached Object
                    var resizer = resizers.Get();

                    await resizer.Convert(raw);

                    Task.Run(() =>
                    {
                        render.Draw(resizer.FrameBuffer);

                        // Return cached object
                        resizers.Return(resizer);
                    });
                }
            }
            catch (EndOfStreamException)
            {

            }
        }

        private static unsafe Bitmap ToBitmap(this ImageData bitmap)
        {
            // Pointers witchcraft here,
            // handle with caution
            fixed (byte* p = bitmap.Data)
            {
                return new Bitmap(bitmap.ImageSize.Width, bitmap.ImageSize.Height, bitmap.Stride, PixelFormat.Format24bppRgb, new IntPtr(p));
            }
        }
    }
}
