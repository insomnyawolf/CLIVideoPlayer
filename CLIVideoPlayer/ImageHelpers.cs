using SixLabors.ImageSharp;
using System.Text;

namespace CLIVideoPlayer;

internal class ImageHelpers
{
    public static int GetEstimatedBufferSize(Size size)
    {
        var pixelCount = size.Width * size.Height;

        var framebufferSize = pixelCount;

#warning Review that
        // But we should most likely use ascii only
        if (GlobalSettings.Encoding == Encoding.ASCII)
        {
            framebufferSize *= sizeof(byte);
        }
        else if (GlobalSettings.Encoding == Encoding.UTF8)
        {
            framebufferSize *= sizeof(byte) * 2;
        }
        else if (GlobalSettings.Encoding == Encoding.Unicode)
        {
            framebufferSize *= sizeof(byte) * 2;
        }

        return framebufferSize;
    }

    public static Size AspectRatioResizeCalculator(Size origin, Size target)
    {
        var width = origin.Width;
        var height = origin.Height;

        decimal coefficientFitWidth = CoefficientChange(width, target.Width);
        decimal coefficientFitHeight = CoefficientChange(height, target.Height);

        decimal coefficient = coefficientFitWidth < coefficientFitHeight ? coefficientFitWidth : coefficientFitHeight;

        // Avoid Upscaling
        if (coefficient > 1)
        {
            return origin;
        }

        width = decimal.ToInt32(coefficient * origin.Width);
        height = decimal.ToInt32(coefficient * origin.Height);

        // Images must have at least 1 px on both sides
        // This fixes it
        coefficient = 0;
        if (width < 1)
        {
            coefficient = CoefficientChange(width, 1);
        }
        else if (height < 1)
        {
            coefficient = CoefficientChange(height, 1);
        }

        if (coefficient != 0)
        {
            height = decimal.ToInt32(coefficient * origin.Width);
            width = decimal.ToInt32(coefficient * origin.Height);
        }

        return new Size
        {
            Width = width,
            Height = height,
        };
    }

    private static decimal CoefficientChange(int valorInicial, int valorFinal)
    {
        return 100M / valorInicial * valorFinal / 100;
    }
}
