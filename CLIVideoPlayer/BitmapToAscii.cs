using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.ObjectPool;

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
        public Stream FrameBuffer { get; set; }

        private static readonly ConversionValue[] ConversionTable = new[]
        {
            new ConversionValue(230, "@"),
            new ConversionValue(200, "#"),
            new ConversionValue(180, "8"),
            new ConversionValue(160, "&"),
            new ConversionValue(130, "O"),
            new ConversionValue(100, ":"),
            new ConversionValue(70, "*"),
            new ConversionValue(50, "."),
            new ConversionValue(-1, " "),
        };

        public static byte[] NewLine = Encoding.UTF8.GetBytes("\n");

        private static byte[] GetGrayCharacter(int brightnessValue)
        {
            // This is slower than if/else chaining but it's a bit more clean
            // Maybe we could try custom source generators
            for (int Index = 0; Index < ConversionTable.Length; Index++)
            {
                var item = ConversionTable[Index];

                if (brightnessValue > item.Threshold)
                {
                    return item.Value;
                }
            }

            throw new Exception($"GetGrayShade for value {brightnessValue}");
        }

        private static int GetGrayShadeWeighted(Bgr24 col)
        {
            // To convert to grayscale, the easiest method is to add
            // the R+G+B colors and divide by three to get the gray
            // scaled color.
            return ((col.R * 30) + (col.G * 59) + (col.B * 11)) / 100;
        }

        public async Task Convert(Image<Bgr24> image)
        {
            // Loop through each pixel in the bitmap
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the color of the current pixel
                    //col = bmp.GetPixel(x, y);
                    var color = image[x, y];

                    // Get the value from the grayscale color,
                    // parse to an int. Will be between 0-255.
                    var shade = GetGrayShadeWeighted(color);

                    // Get the bytes that represent the value we found
                    // in our cache list
                    var bytes = GetGrayCharacter(shade);

                    // Append the bytes
                    await FrameBuffer.WriteAsync(bytes);
                }

                // Append new line because it doesn't look right otherwise
                await FrameBuffer.WriteAsync(NewLine);
            }

            FrameBuffer.Position = 0;
        }
    }
}
