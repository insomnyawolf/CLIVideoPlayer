using Microsoft.Extensions.ObjectPool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

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
    private static readonly ConcurrentDictionary<Vector4, byte[]> ColorCache = new();

    public static string NewLine = "\n";
    public static byte[] NewLineBytes = Encoding.UTF8.GetBytes(NewLine);

    public static string Pixel = " ";
    public static byte[] PixelBytes = Encoding.UTF8.GetBytes(Pixel);

    public static byte[] GetBytes(Vector4 color)
    {
        var colorRaw = new Rgba32();
        colorRaw.FromVector4(color);
        return Encoding.UTF8.GetBytes($"\x1b[48;2;{colorRaw.R};{colorRaw.G};{colorRaw.B}m{Pixel}");
    }

    public MemoryStream FrameBuffer { get; set; }

    public void Convert(Image<Bgr24> image)
    {
        Vector4? lastColor = null;

        var total = image.Height * image.Width;

        var currentX = 0;

        image.DangerousTryGetSinglePixelMemory(out var memory);

        var span = memory.Span;

        // Loop through each pixel in the bitmap
        for (int i = 0; i < total; i++)
        {
            if (currentX == image.Width)
            {
                currentX = 0;
                // Append new line because it doesn't look right otherwise
                FrameBuffer.Write(NewLineBytes);
            }

            // Get the color of the current pixel
            // And convert it to a vector
            var color = span[i].ToVector4();

            if (color == lastColor)
            {
                // No Color Update, It's the same
                FrameBuffer.Write(PixelBytes);
            }
            else
            {
                // Cache Colors
                var colorBytes = ColorCache.GetOrAdd(key: color, valueFactory: GetBytes);

                // Append the color change and the pixel
                FrameBuffer.WriteAsync(colorBytes);
            }

            currentX++;
        }

        FrameBuffer.Position = 0;
    }
}
