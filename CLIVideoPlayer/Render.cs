using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace CLIVideoPlayer
{

    // Considerate to save previous frame and only edit needed pixels
    public class Render
    {
        public Stream StdOut { get; set; } = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);
        //public Stream StdOut { get; set; } = File.OpenWrite("test.txt");
        public Stopwatch Watch { get; } = Stopwatch.StartNew();
        public long FrameRenderDelay { get; set; } = 0;

        private char[] FrameBuffer;

        private Size RenderSize;

        public Render(Size RenderSize)
        {
            this.RenderSize = RenderSize;
            this.FrameBuffer = new char[RenderSize.Width * RenderSize.Height];
        }

        public void NextFrame(string content, int verticalOffset = 0)
        {
            Watch.Restart();

            Console.SetCursorPosition(0, verticalOffset);

            var bytes = Encoding.ASCII.GetBytes(content + "\n");

            StdOut.Write(bytes, 0, bytes.Length);

            FrameRenderDelay = Watch.ElapsedMilliseconds;
        }

        public void NextDiffFrame(ref char[] content, int verticalOffset = 0)
        {
            Watch.Restart();

            bool isDifferent = false;

            int startingPoint = 0;

            for (int x = 0; x < FrameBuffer.Length; x++)
            {
                if (content[x] != FrameBuffer[x])
                {
                    if (!isDifferent)
                    {
                        isDifferent = true;
                        startingPoint = x;
                    }

                    FrameBuffer[x] = content[x];
                }
                else if (content[x] == FrameBuffer[x] && isDifferent)
                {
                    DrawChunk(content[startingPoint..(x-1)], startingPoint);
                }
                else
                {
                    // Everything is equal
                }
            }

            //if (isDifferent)
            {
                DrawChunk(content[startingPoint..content.Length], startingPoint);
            }

            //Console.SetCursorPosition(0, verticalOffset);

            //var bytes = Encoding.ASCII.GetBytes(content + "\n");

            //StdOut.Write(bytes, 0, bytes.Length);

            FrameBuffer = content;

            FrameRenderDelay = Watch.ElapsedMilliseconds;
        }

        private void DrawChunk(char[] content, int position)
        {
            var coords = CalculateCoords(position);
            Console.SetCursorPosition(coords.Item1, coords.Item2);

            var bytes = Encoding.UTF8.GetBytes(content);
            StdOut.Write(bytes, 0, bytes.Length);

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
