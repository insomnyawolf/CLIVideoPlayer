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

            // Render.Title($"ShellEngine Test, Playing {Path.GetFileName(filePath)} at {framerate} fps");

            double framePeriod = 1000 / framerate;

            // edit this if the image is too small or too big and makes earthquakes
            int safeArea = 1;

            var size = new Size(Console.WindowWidth - safeArea, Console.WindowHeight - safeArea);

            var temp = file.Video.GetFrame(TimeSpan.Zero).ToBitmap();

            size = BulkImageResizer.AspectRatioResizeCalculator(temp.Size, size);

            var bulkResize = new BulkImageResizer(size, temp.HorizontalResolution, temp.VerticalResolution, ResizerQuality.HighSpeed);

            // +1 for the linebreaks
            size.Width += 1;

            char[] frameBuffer = new char[size.Width * size.Height];

            for(int i = size.Width - 1; i < frameBuffer.Length; i+= size.Width)
            {
                frameBuffer[i] = '\n';
            }

            Render render = new Render(size);

            var watch = Stopwatch.StartNew();

            long frameDecodingDelay;
            int frameDelay;

            try
            {
                for (int i = 0; i < file.Video.Info.NumberOfFrames.Value; i++)
                {
                    // I don't use TryGetNextFrame because internally does the same and doesn't let me use async code

                    watch.Restart();

                    var raw = file.Video.GetNextFrame().ToBitmap();

                    var resized = bulkResize.Resize(raw);

                    BitmapToAscii.UpdateFrameBuffer(resized, ref frameBuffer);

                    frameDecodingDelay = watch.ElapsedMilliseconds;

                    frameDelay = (int)(framePeriod - frameDecodingDelay - render.FrameRenderDelay);

                    // FpsCounters are harder than i remember => it doesnh't take into account the frame delay
                    var tempTimer = framePeriod - frameDelay;
                    Console.Title = $"Playing: '{filePath}' at {(tempTimer <= 0 ? 0 : 1000d/tempTimer)}fps";

                    if (frameDelay < 0)
                    {
                        frameDelay = 0;
                    }

                    await Task.Delay(frameDelay);

                    // I don't understand why or how this works, but it does (maybe)
                    // It causes weird glitches when render times is higer than decoding delay

                    Task.Run(() => render.NextDiffFrame(ref frameBuffer));

                    //render.NextDiffFrame(ref frameBuffer);
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
