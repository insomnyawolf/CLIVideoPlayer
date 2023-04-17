using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public class Render
    {
        private Stopwatch Watch { get; } = Stopwatch.StartNew();
        public TimeSpan FrameTime => Watch.Elapsed;
        public TimeSpan TargetFrameTime { get; set; }
        public double TargetFramerate 
        {
            get
            {
                return 1000 / TargetFrameTime.Milliseconds;
            }
            set
            {
                TargetFrameTime = TimeSpan.FromMilliseconds(1000 / value);
            } 
        }
        public Stream StdOut { get; set; }

        public Render()
        {
            this.StdOut = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);
        }

        public async Task Draw(Stream FrameBuffer)
        {
            // black magic to limit the framerate
            var delayNeeded = TargetFrameTime - FrameTime;
            
            if (delayNeeded > TimeSpan.Zero)
            {
                await Task.Delay(delayNeeded);
            }

            // fps conter at the title bar
            // must be after the delay to properly work
            if (Watch.ElapsedMilliseconds != 0)
            {
                var fps = 1000d / Watch.ElapsedMilliseconds;
                Console.Title = $"FPS => {fps:.000}";
            }

            Watch.Restart();

            Console.SetCursorPosition(0, Console.CursorTop);

            await FrameBuffer.CopyToAsync(StdOut);
        }
    }
}
