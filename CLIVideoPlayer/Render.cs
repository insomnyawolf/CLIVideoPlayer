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

            this.StdOut = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);

            reservedMemory = ArrayPool<byte>.Create();
        }

        public void NextDiffFrame(ref char[] content, int verticalOffset = 0)
        {
            Watch.Restart();

            // this doesn't work yet

            //bool previousIsDifferent = false;

            //int startingPoint = 0;

            //for (int x = 0; x < FrameBuffer.Length; x++)
            //{
            //    var oldData = FrameBuffer[x];
            //    var newData = content[x];

            //    var currentlyEquals = oldData == newData;
            //    if (!currentlyEquals)
            //    {
            //        if (!previousIsDifferent)
            //        {
            //            previousIsDifferent = true;
            //            startingPoint = x;
            //        }
            //    }
            //    else if (currentlyEquals && previousIsDifferent)
            //    {
            //        var endingPoint = x - 1;

            //        if (startingPoint == 0)
            //        {
            //            Array.Copy(sourceArray: content, sourceIndex: startingPoint, destinationArray: FrameBuffer, destinationIndex: startingPoint, length: endingPoint);
            //        }

            //        DrawChunk(content, startingPoint, endingPoint);
            //    }
            //    else
            //    {
            //        //Everything is equal
            //    }
            //}

            //if (previousIsDifferent)
            {
                //if (startingPoint == 0)
                //{
                //    Array.Copy(sourceArray: content, sourceIndex: 0, destinationArray: FrameBuffer, destinationIndex: 0, length: content.Length);
                //}

                //DrawChunk(content, startingPoint, content.Length);
            }

            DrawChunk(content, 0, content.Length);
            FrameRenderDelay = Watch.ElapsedMilliseconds;
        }

        private void DrawChunk(char[] content, int start, int end)
        {
            var coords = CalculateCoords(start);
            Console.SetCursorPosition(coords.Item1, coords.Item2);

            // How does this even work??? ↓

            var copylenght = end - start;

            var bytes = reservedMemory.Rent(copylenght * 2);
            var lenght = Encoding.UTF8.GetBytes(content, start, copylenght, bytes, 0);
            StdOut.Write(bytes, 0, lenght);
            reservedMemory.Return(bytes, false);
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
