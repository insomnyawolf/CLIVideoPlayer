using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace CLIVideoPlayer;

public class Render
{
    [UnsafeAccessor(kind: UnsafeAccessorKind.StaticField, Name = "s_tickFrequency")]
    public static extern ref double GetHigResTicksPerSecond(Stopwatch Watch);
    private static readonly double TicksPerSecond = GetHigResTicksPerSecond(null);
    private static readonly double TicksPerMiliSecond = TicksPerSecond * 1000;

    private readonly Stopwatch Watch = Stopwatch.StartNew();
    public int TargetFrameTime { get; set; }
    public double TargetFramerate
    {
        get
        {
            return 1000 / TargetFrameTime;
        }
        set
        {
            TargetFrameTime = (int)(1000 / value);
        }
    }
    public Stream StdOut { get; set; }

    public static readonly ReadOnlyMemory<byte> CursorReset = GlobalSettings.Encoding.GetBytes("\x1b[99999;99999");
    public static readonly ReadOnlyMemory<byte> CursorSavePos = GlobalSettings.Encoding.GetBytes("\x1b[s");
    public static readonly ReadOnlyMemory<byte> CursorLoadPos = GlobalSettings.Encoding.GetBytes("\x1b[u");
    public static readonly ReadOnlyMemory<byte> ClearScreen = GlobalSettings.Encoding.GetBytes("\x1b[2J");

    public Render()
    {
#if true //&& !true
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
        var elapsedMs = Watch.ElapsedTicks / TicksPerMiliSecond;
        var delayNeeded = TargetFrameTime - elapsedMs;

        // FrameLimiter & FpsDisplay are broken
        //if (delayNeeded > 0)
        //{
        //    await Task.Delay((int)delayNeeded);
        //}

        // fps conter at the title bar
        // must be after the delay to properly work
        elapsedMs = Watch.ElapsedTicks / TicksPerMiliSecond;
        var fps = 10000d / elapsedMs;
        Console.Title = $"FPS => {fps:.000}";

        Watch.Restart();

#warning test properly
        await StdOut.WriteAsync(CursorLoadPos);
        await FrameBuffer.CopyToAsync(StdOut);

        // // This Destroys performance (?)
        //StdOut.Write(CursorLoadPos.Span);
        //FrameBuffer.CopyTo(StdOut);
    }
}
