using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace CLIVideoPlayer
{
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
        static readonly Bgr24 Black = new(0, 0, 0);
        static readonly byte[] BlackBytes = GetBytes(Black);
        private static readonly ConcurrentDictionary<Bgr24, byte[]> ColorCache = new ();
        public static byte[] NewLine = Encoding.UTF8.GetBytes("\n");
        //public static byte[] Pixel = Encoding.UTF8.GetBytes("█");
        public static byte[] Pixel = Encoding.UTF8.GetBytes(" ");
        //public static byte[] Pixel = Encoding.UTF8.GetBytes("\uFF00");

        public static byte[] GetBytes(Bgr24 color)
        {
            return Encoding.UTF8.GetBytes($"\x1b[48;2;{color.R};{color.G};{color.B}m");
        }

        public Stream FrameBuffer { get; set; }

        public async Task Convert(Image<Bgr24> image)
        {
            Bgr24? lastColor = null;

            // Loop through each pixel in the bitmap
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the color of the current pixel
                    //col = bmp.GetPixel(x, y);
                    var color = image[x, y];

                    if (color != lastColor)
                    {
                        if (!ColorCache.TryGetValue(color, out var value))
                        {
                            value = GetBytes(color);

                            ColorCache.TryAdd(color, value);
                        }

                        await FrameBuffer.WriteAsync(value);
                    }

                    // Append the bytes
                    await FrameBuffer.WriteAsync(Pixel);
                }

                // Append new line because it doesn't look right otherwise
                //await FrameBuffer.WriteAsync(BlackBytes);
                await FrameBuffer.WriteAsync(NewLine);
            }

            FrameBuffer.Position = 0;
        }
    }
}
