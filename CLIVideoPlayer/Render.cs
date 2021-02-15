using System;

namespace CLIVideoPlayer
{
    public static class Render
    {
        public static void Title(string content)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(content);
        }

        public static void NextFrame(string content)
        {
            Console.SetCursorPosition(0, 1);
            Console.WriteLine(content);
        }
    }
}
