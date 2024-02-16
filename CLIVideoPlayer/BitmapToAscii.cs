using Microsoft.Extensions.ObjectPool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CLIVideoPlayer;

// Yay, no reallocations \:D/
public class BitmapToAsciiPooledObjectPolicy : PooledObjectPolicy<BitmapToAscii>
{
    public int CacheDefaultCapacity { get; set; }
    public override BitmapToAscii Create()
    {
        return new BitmapToAscii(new MemoryStream(CacheDefaultCapacity));
    }

    public override bool Return(BitmapToAscii obj)
    {
        // Reset the buffer cursor so we can reuse it as if it were new without allocating new memory

        var buffer = obj.FrameBuffer;

        buffer.SetLength(0);

        return true;
    }
}

public unsafe class BitmapToAscii
{
    public static readonly ReadOnlyMemory<byte> NewLine = Render.Encoding.GetBytes("\n");

    public static readonly ReadOnlyMemory<byte> Pixel = Render.Encoding.GetBytes(" ");

    // "\x1b[48;2;{color.R};{color.G};{color.B}m{Pixel}"
    public static readonly ReadOnlyMemory<byte> ColorChange = Render.Encoding.GetBytes("\x1b[48;2;");

    public static readonly ReadOnlyMemory<byte> Semicolon = Render.Encoding.GetBytes(";");

    public static readonly ReadOnlyMemory<byte> CharM = Render.Encoding.GetBytes("m");

    [UnsafeAccessor(kind: UnsafeAccessorKind.Field, Name = "frames")]
    public static extern ref ImageFrameCollection<Bgr24> GetFrames(Image<Bgr24> image);

    private static readonly ReadOnlyMemory<byte>[] NumberCache = new ReadOnlyMemory<byte>[256];
    //private static readonly ReadOnlyMemory<byte>* NumberCacheRef;
    static BitmapToAscii()
    {
        for (var i = 0; i < NumberCache.Length; i++)
        {
            NumberCache[i] = Render.Encoding.GetBytes(i + "");
        }

        //NumberCacheRef = (ReadOnlyMemory<byte>*)Unsafe.AsPointer(ref NumberCache[0]);
    }
    public MemoryStream FrameBuffer { get; init; }
    public StreamWriter StreamWriter { get; init; }

    public BitmapToAscii(MemoryStream FrameBuffer)
    {
        this.FrameBuffer = FrameBuffer;
        this.StreamWriter = new StreamWriter(stream: FrameBuffer, encoding: null, bufferSize: 128, leaveOpen: true);
    }

    public void Convert(Image<Bgr24> image)
    {
        Bgr24 lastColor = new Bgr24();
        // That's more performant than having the nullable
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

                    ////Slower than the alternative
                    //var current = *(int*)&color;
                    //var old = *(int*)&lastColor;
                    //if (current != old)

                    // Nullable performance hit is freaking scary lol
                    // if (/*lastColor is null || */!IsSameColor(color, lastColor))

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