using System;
using System.IO;
using System.Text;

namespace CLIVideoPlayer
{
    public static class Render
    {
        private static readonly Stream StdOut = Console.OpenStandardOutput(Console.BufferWidth * Console.BufferHeight);

        public static void NextFrame(string content, int verticalOffset = 0)
        {
            Console.SetCursorPosition(0, verticalOffset);

            var bytes = Encoding.ASCII.GetBytes(content);

            StdOut.Write(bytes, 0, bytes.Length);
        }
    }
}
