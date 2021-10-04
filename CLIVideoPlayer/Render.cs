using System;
using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace CLIVideoPlayer
{
    public class Render
    {
        public Stream StdOut { get; set; }
        private Stopwatch Watch { get; } = Stopwatch.StartNew();
        public long FrameRenderDelay { get; set; } = 0;

        private ArrayPool<byte> reservedMemory;

        private char[] FrameBuffer;

        private Size RenderSize;

        public Render(Size RenderSize)
        {
            this.RenderSize = RenderSize;
            this.FrameBuffer = new char[RenderSize.Width * RenderSize.Height];

            for (int i = 0; i < this.FrameBuffer.Length; i++)
            {
                this.FrameBuffer[0] = 'P';
            }

            this.StdOut = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);

            reservedMemory = ArrayPool<byte>.Create();
        }

        public void NextFrame(string content, int verticalOffset = 0)
        {
            Watch.Restart();

            Console.SetCursorPosition(0, verticalOffset);

            var bytes = Encoding.ASCII.GetBytes(content + "\n");

            StdOut.Write(bytes, 0, bytes.Length);

            FrameRenderDelay = Watch.ElapsedMilliseconds;
        }

        public void NextDiffFrame(char[] content, int verticalOffset = 0)
        {
            Watch.Restart();

            bool isDifferent = false;

            int startingPoint = 0;

            for (int x = 0; x < FrameBuffer.Length; x++)
            {
                var equals = content[x].Equals(FrameBuffer[x]);
                if (!equals)
                {
                    if (!isDifferent)
                    {
                        isDifferent = true;
                        startingPoint = x;
                    }

                    FrameBuffer[x] = content[x];
                }
                else if (equals && isDifferent)
                {

                    DrawChunk(ref content, startingPoint, x - 1);
                }
                else
                {
                    // Everything is equal
                }
            }

            if (isDifferent)
            {
                DrawChunk(ref content, startingPoint, content.Length);
            }

            FrameBuffer = content;

            FrameRenderDelay = Watch.ElapsedMilliseconds;
        }

        private void DrawChunk(ref char[] content, int start, int end)
        {
            var coords = CalculateCoords(start);
            Console.SetCursorPosition(coords.Item1, coords.Item2);

            // How does this even work??? ↓

            var count = end - start;

            var bytes = reservedMemory.Rent(count * 2);
            var lenght = Encoding.UTF8.GetBytes(content, start, count, bytes, 0);
            StdOut.Write(bytes, 0, lenght);
        }

        private (int, int) CalculateCoords(int pos)
        {
            int y = 0;

            while (pos > RenderSize.Width)
            {
                pos -= RenderSize.Width;
                y++;
            }

            return (pos, y);
        }
    }
}
