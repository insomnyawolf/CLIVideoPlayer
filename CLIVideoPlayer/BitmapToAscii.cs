using Microsoft.Extensions.ObjectPool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
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
        return new BitmapToAscii()
        {
            FrameBuffer = new MemoryStream(CacheDefaultCapacity),
            //ColorCache = new Dictionary<Vector4, byte[]>(),
        };
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
    public static string NewLine = "\n";
    public static byte[] NewLineBytes = Encoding.UTF8.GetBytes(NewLine);

    public static string Pixel = " ";
    public static byte[] PixelBytes = Encoding.UTF8.GetBytes(Pixel);

    public MemoryStream FrameBuffer { get; init; }
    public static Dictionary<Bgr24, byte[]> ColorCache { get; } = new();

    public static byte[] GetBytes(Bgr24 color)
    {
        return Encoding.UTF8.GetBytes($"\x1b[48;2;{color.R};{color.G};{color.B}m{Pixel}");
    }

    [UnsafeAccessor(kind: UnsafeAccessorKind.Field, Name = "frames")]
    public static extern ref ImageFrameCollection<Bgr24> GetFrames(Image<Bgr24> image);

    public unsafe void Convert(Image<Bgr24> image)
    {
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

                if (color == lastColor)
                {
                    // No Color Update, It's the same
                    FrameBuffer.Write(PixelBytes);
                }
                else
                {
                    lastColor = color;
                    // Cache Colors
                    lock (ColorCache)
                    {
                        ref var colorBytes = ref CollectionsMarshal.GetValueRefOrAddDefault(ColorCache, color, out var exists);
                        if (!exists)
                        {
                            colorBytes = GetBytes(color);
                        }

                        // Append the color change and the pixel
                        FrameBuffer.WriteAsync(colorBytes);
                    }
                }

                position = ref Unsafe.Add(ref position, 1);
            }

            // Append new line because it doesn't look right otherwise
            FrameBuffer.Write(NewLineBytes);
        }

        FrameBuffer.Position = 0;
    }
}
