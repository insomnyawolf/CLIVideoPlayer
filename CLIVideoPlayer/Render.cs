using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public class Render
    {
        public Stream StdOut { get; set; }

        public Render(Size RenderSize)
        {
            this.StdOut = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);
        }

        public async Task Draw(Stream FrameBuffer)
        {
            Console.SetCursorPosition(0, Console.CursorTop);

            await FrameBuffer.CopyToAsync(StdOut);
        }
    }
}
