using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CLIVideoPlayer;

public unsafe class BitmapToAscii
{
    // "\x1b[48;2;{color.R};{color.G};{color.B}m{Pixel}"
    public static readonly ReadOnlyMemory<byte> NewLine = GlobalSettings.Encoding.GetBytes("\n");

    public static readonly ReadOnlyMemory<byte> Pixel = GlobalSettings.Encoding.GetBytes(" ");

    public static readonly ReadOnlyMemory<byte> ColorChange = GlobalSettings.Encoding.GetBytes("\x1b[48;2;");

    public static readonly ReadOnlyMemory<byte> Semicolon = GlobalSettings.Encoding.GetBytes(";");

    public static readonly ReadOnlyMemory<byte> CharM = GlobalSettings.Encoding.GetBytes("m");

    [UnsafeAccessor(kind: UnsafeAccessorKind.Field, Name = "frames")]
    public static extern ref ImageFrameCollection<Bgr24> GetFrames(Image<Bgr24> image);

    private static readonly ReadOnlyMemory<byte>[] NumberCache = new ReadOnlyMemory<byte>[256];

    static BitmapToAscii()
    {
        for (var i = 0; i < NumberCache.Length; i++)
        {
            NumberCache[i] = GlobalSettings.Encoding.GetBytes(i + "");
        }
    }

    // Instance

    public readonly MemoryStream FrameBuffer;
    public readonly StreamWriter StreamWriter;

    public BitmapToAscii(MemoryStream FrameBuffer)
    {
        this.FrameBuffer = FrameBuffer;
        this.StreamWriter = new StreamWriter(stream: FrameBuffer, encoding: null, bufferSize: 128, leaveOpen: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Convert(Image<Bgr24> image)
    {
        Bgr24 lastColor = new Bgr24();
        // That's more performant than having the nullable by a lot
        WriteColor(lastColor);

        var frames = GetFrames(image);
        var rf = frames.RootFrame;
        var pixelBuffer = rf.PixelBuffer;

        // Loop through each pixel in the bitmap
        for (int y = 0; y < image.Height; y++)
        {
            var rowSpan = pixelBuffer.DangerousGetRowSpan(y);

            fixed (Bgr24* altItems = &rowSpan[0])
            {
                var position = 0;

                while (position < rowSpan.Length)
                {
                    Bgr24 color = altItems[position];

                    if (!IsSameColor(color, lastColor))
                    {
                        lastColor = color;

                        WriteColor(color);
                    }

                    // Add the pixel
                    FrameBuffer.Write(Pixel.Span);

                    position++;
                }

                // Append new line because it doesn't look right otherwise
                FrameBuffer.Write(NewLine.Span);
            }
        }

        FrameBuffer.Position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void WriteColor(Bgr24 color)
    {
        // Add the color Change
        FrameBuffer.Write(ColorChange.Span);

        var R = NumberCache[color.R];
        FrameBuffer.Write(R.Span);

        FrameBuffer.Write(Semicolon.Span);

        var G = NumberCache[color.G];
        FrameBuffer.Write(G.Span);

        FrameBuffer.Write(Semicolon.Span);

        var B = NumberCache[color.B];
        FrameBuffer.Write(B.Span);

        FrameBuffer.Write(CharM.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsSameColor(Bgr24 a, Bgr24 b)
    {
        var result = a.R == b.R &&
                     a.G == b.G &&
                     a.B == b.B;
        return result;
    }
}