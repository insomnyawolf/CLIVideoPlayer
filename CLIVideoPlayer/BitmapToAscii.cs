using Microsoft.Extensions.ObjectPool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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

public class ConversionValue
{
    public int Threshold { get; }
    public byte[] Value { get; }
    public ConversionValue(int Threshold, string Value)
    {
        this.Threshold = Threshold;
        this.Value = Encoding.UTF8.GetBytes(Value);
    }
}

public class BitmapToAscii
{
    public static readonly byte[] NewLine = Encoding.UTF8.GetBytes("\n");

    public static readonly byte[] Pixel = Encoding.UTF8.GetBytes(" ");

    // "\x1b[48;2;{color.R};{color.G};{color.B}m{Pixel}"
    public static readonly byte[] ColorChange = Encoding.UTF8.GetBytes("\x1b[48;2;");

    public static readonly byte[] Semicolon = Encoding.UTF8.GetBytes(";");

    public static readonly byte[] CharM = Encoding.UTF8.GetBytes("m");

    [UnsafeAccessor(kind: UnsafeAccessorKind.Field, Name = "frames")]
    public static extern ref ImageFrameCollection<Bgr24> GetFrames(Image<Bgr24> image);

    private static readonly byte[][] NumberCache = new byte[256][];
    static BitmapToAscii()
    {
        for (var i = 0; i < NumberCache.Length; i++)
        {
            NumberCache[i] = Encoding.UTF8.GetBytes(i + "");
        }
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
        ref var numberCacheRef = ref MemoryMarshal.GetArrayDataReference(NumberCache);

        Bgr24? lastColor = null;

        var frames = GetFrames(image);
        var rf = frames.RootFrame;
        var pixelBuffer = rf.PixelBuffer;

        // Loop through each pixel in the bitmap
        for (int y = 0; y < image.Height; y++)
        {
            var row = pixelBuffer.DangerousGetRowSpan(y);

            ref var position = ref MemoryMarshal.GetReference(row);
            ref var end = ref Unsafe.Add(ref position, row.Length);

            while (Unsafe.IsAddressLessThan(ref position, ref end))
            {
                var color = position;

                if (color != lastColor)
                {
                    lastColor = color;

                    // Add the color Change
                    FrameBuffer.Write(ColorChange);

                    ref var R = ref Unsafe.Add(ref numberCacheRef, color.R);
                    FrameBuffer.Write(R);

                    FrameBuffer.Write(Semicolon);

                    ref var G = ref Unsafe.Add(ref numberCacheRef, color.G);
                    FrameBuffer.Write(G);

                    FrameBuffer.Write(Semicolon);

                    ref var B = ref Unsafe.Add(ref numberCacheRef, color.B);
                    FrameBuffer.Write(B);

                    FrameBuffer.Write(CharM);
                }

                // Add the pixel
                FrameBuffer.Write(Pixel);

                position = ref Unsafe.Add(ref position, 1);
            }

            // Append new line because it doesn't look right otherwise
            FrameBuffer.Write(NewLine);
        }

        FrameBuffer.Position = 0;
    }
}

public static class Extensions
{
    public static string ConvertToBase64(this MemoryStream stream)
    {
        byte[] bytes = stream.ToArray();
        string base64 = Convert.ToBase64String(bytes);
        return base64;
    }
}