using FastBitmapLib;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public class ConversionValue
    {
        public int Threshold { get; }
        public byte[] Value { get;}
        public ConversionValue(int Threshold, string Value) 
        {
            this.Threshold = Threshold;
            this.Value = Encoding.UTF8.GetBytes(Value);
        }
    }
    public class BitmapToAscii
    {
        private static readonly ConversionValue[] ConversionTable = new [] 
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

        private static byte[] GetGrayCharacter(int redValue)
        {
            // This is slower than if/else chaining but it's a bit more clean
            for (int Index = 0; Index < ConversionTable.Length; Index++)
            {
                var item = ConversionTable[Index];

                if (redValue > item.Threshold)
                {
                    return item.Value;
                }
            }

            throw new Exception($"GetGrayShade for value {redValue}");
        }

        private static int GetGrayShadeWeighted(Color col)
        {
            // To convert to grayscale, the easiest method is to add
            // the R+G+B colors and divide by three to get the gray
            // scaled color.
            return ((col.R * 30) + (col.G * 59) + (col.B * 11)) / 100;
        }

        // Yay, no allocations \:D/
        public static async Task UpdateFrameBuffer(Bitmap bmp, Stream frameBuffer)
        {
            Color col;

            using var fb = bmp.FastLock();

            // Loop through each pixel in the bitmap
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    // Get the color of the current pixel
                    //col = bmp.GetPixel(x, y);
                    col = fb.GetPixel(x, y);

                    // Get the value from the grayscale color,
                    // parse to an int. Will be between 0-255.
                    var shade = GetGrayShadeWeighted(col);

                    // Get the bytes that represent the value we found
                    // in our cache list
                    var bytes = GetGrayCharacter(shade);

                    // Append the bytes
                    await frameBuffer.WriteAsync(bytes);
                }

                // Append new line because it doesn't look right otherwise
                await frameBuffer.WriteAsync(NewLine);
            }

            frameBuffer.Position = 0;
        }
    }
}
