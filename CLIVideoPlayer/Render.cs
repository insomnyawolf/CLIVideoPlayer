using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CLIVideoPlayer;

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

    public static readonly ReadOnlyMemory<byte> CursorReset = GlobalSettings.Encoding.GetBytes("\x1b[99999;99999");
    public static readonly ReadOnlyMemory<byte> CursorSavePos = GlobalSettings.Encoding.GetBytes("\x1b[s");
    public static readonly ReadOnlyMemory<byte> CursorLoadPos = GlobalSettings.Encoding.GetBytes("\x1b[u");
    public static readonly ReadOnlyMemory<byte> ClearScreen = GlobalSettings.Encoding.GetBytes("\x1b[2J");

    public Render()
    {
#if true && !true
        this.StdOut = Console.OpenStandardOutput();
#else
        // With this i can prove that the bottleneck is the windows console
        this.StdOut = Stream.Null;
#endif

        //var handle = FastConsole.CreateOutputHandle();
        //var fs = new FileStream(handle, FileAccess.ReadWrite);
        //this.StdOut = fs;

        StdOut.Write(CursorSavePos.Span);
        StdOut.Flush();
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

#warning test properly
        //await StdOut.WriteAsync(CursorLoadPos);
        //await FrameBuffer.CopyToAsync(StdOut);

        // // This Destroys performance (?)
        StdOut.Write(CursorLoadPos.Span);
        FrameBuffer.CopyTo(StdOut);
    }
}
