using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

    // By using ascii we improve the performance a lot, we can still represent every color but we only need to write half of the bytes
    // Also since ascii has a fixed size, we can calculate the buffers and allocate them in the required size directly
    public static readonly Encoding Encoding = Encoding.ASCII;
    //public static readonly Encoding Encoding = Encoding.Unicode;
    public static readonly byte[] CursorReset = Encoding.GetBytes("\x1b[99999;99999");
    public static readonly byte[] CursorSavePos = Encoding.GetBytes("\x1b[s");
    public static readonly byte[] CursorLoadPos = Encoding.GetBytes("\x1b[u");
    public static readonly byte[] ClearScreen = Encoding.GetBytes("\x1b[2J");

    public Render()
    {
        //this.StdOut = Console.OpenStandardOutput();

        // With this i can prove that the bottleneck is the windows console
        this.StdOut = Stream.Null;

        //var handle = FastConsole.CreateOutputHandle();
        //var fs = new FileStream(handle, FileAccess.ReadWrite);
        //this.StdOut = fs;

        StdOut.Write(CursorSavePos);
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

        //Console.SetCursorPosition(0, Console.CursorTop);

        await StdOut.WriteAsync(CursorLoadPos);

        await FrameBuffer.CopyToAsync(StdOut);
    }
}
