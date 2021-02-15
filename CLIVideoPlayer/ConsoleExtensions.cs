using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace CLIVideoPlayer
{
    public static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 SetCurrentConsoleFontEx(
            IntPtr ConsoleOutput,
            bool MaximumWindow,
            ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Int32 GetCurrentConsoleFontEx(
            IntPtr ConsoleOutput,
            bool MaximumWindow,
            ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

        private enum StdHandle
        {
            OutputHandle = -11
        }

        [DllImport("kernel32")]
        private static extern IntPtr GetStdHandle(StdHandle index);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public static void PrepareConsole(short pixelSize)
        {

            // Setting the font and fontsize
            // Other values can be changed too
            // Instantiating CONSOLE_FONT_INFO_EX and setting its size (the function will fail otherwise)
            CONSOLE_FONT_INFO_EX ConsoleFontInfo = new CONSOLE_FONT_INFO_EX();
            ConsoleFontInfo.cbSize = (uint)Marshal.SizeOf(ConsoleFontInfo);
            windowHandle = GetStdHandle(StdHandle.OutputHandle);

            // Optional, implementing this will keep the fontweight and fontsize from changing
            // See notes
            GetCurrentConsoleFontEx(windowHandle, false, ref OldValues);

            //ConsoleFontInfo.FaceName = "Lucida Console";

            ConsoleFontInfo.dwFontSize.X = pixelSize;
            ConsoleFontInfo.dwFontSize.Y = pixelSize;

            SetCurrentConsoleFontEx(windowHandle, false, ref ConsoleFontInfo);

            DefaultConsoleWindowWidth = Console.WindowWidth;
            DefaultConsoleWindowHeight = Console.WindowHeight;
            DefaultConsoleWindowWidthBuffer = Console.BufferWidth;
            DefaultConsoleWindowHeightBuffer = Console.BufferHeight;

            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;

            Console.SetBufferSize(Console.WindowWidth + 1, Console.WindowHeight + 1);

            Console.SetWindowPosition(0, 0);

        }

        private static CONSOLE_FONT_INFO_EX OldValues;
        private static IntPtr windowHandle;

        private static int DefaultConsoleWindowWidth;
        private static int DefaultConsoleWindowHeight;
        private static int DefaultConsoleWindowWidthBuffer;
        private static int DefaultConsoleWindowHeightBuffer;


        public static void RestoreConsole()
        {
            SetCurrentConsoleFontEx(windowHandle, false, ref OldValues);


#warning Doesn't work as intended :(
            //Console.WindowWidth = DefaultConsoleWindowWidth;
            //Console.WindowHeight = DefaultConsoleWindowHeight;

            //Console.SetBufferSize(DefaultConsoleWindowWidthBuffer, DefaultConsoleWindowHeightBuffer);
        }


        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;

            public COORD(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CONSOLE_FONT_INFO_EX
        {
            public uint cbSize;
            public uint nFont;
            public COORD dwFontSize;
            public int FontFamily;
            public int FontWeight;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] // Edit sizeconst if the font name is too big
            public string FaceName;
        }

        static public T DeepCopy<T>(T obj)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obj);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }
    }
}