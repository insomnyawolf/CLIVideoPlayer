using SixLabors.ImageSharp;
using System;

namespace CLIVideoPlayer;

#warning try to autodettect those values
internal class ConsoleHelpers
{
    public static Size GetScaledToConsoleAspectRatio(Size size)
    {
        var videoSizeAdjustedToConsole = size;

        videoSizeAdjustedToConsole.Width *= 2;

        return videoSizeAdjustedToConsole;
    }

    public static Size GetConsoleSafeArea()
    {
        // edit this if the image is too small or too big and makes earthquakes
        const int safeArea = 1;

        var consoleSize = new Size(Console.WindowWidth - safeArea, Console.WindowHeight - safeArea);

        return consoleSize;
    }
}
