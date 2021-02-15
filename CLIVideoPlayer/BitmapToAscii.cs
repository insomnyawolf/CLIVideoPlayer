using FastBitmapLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public class BitmapToAscii
    {
        private StringBuilder @string;

        public BitmapToAscii()
        {
            @string = new StringBuilder();
        }

        private static readonly KeyValuePair<int, char>[] conversion = new KeyValuePair<int, char>[9]
        {
            new KeyValuePair<int, char>(230, ' '),
            new KeyValuePair<int, char>(200, '·'),
            new KeyValuePair<int, char>(180, '*'),
            new KeyValuePair<int, char>(160, ':'),
            new KeyValuePair<int, char>(130, 'O'),
            new KeyValuePair<int, char>(100, '&'),
            new KeyValuePair<int, char>(70, '8'),
            new KeyValuePair<int, char>(50, '#' ),
            new KeyValuePair<int, char>(-1, '@' ),
        };

        private static char GetGrayShade(int redValue)
        {
            for (int i = 0; i < conversion.Length; i++)
            {
                if (256 - redValue > conversion[i].Key)
                {
                    return conversion[i].Value;
                }
            }

            throw new Exception($"GetGrayShade for value {redValue}");
        }

        private static Color ConvertToGrayScale(Color col)
        {
            var value = (col.R + col.G + col.B) / 3;
            return Color.FromArgb(value, value, value);
        }

        private static int GetGrayScaleBrightness(Color col)
        {
            return (col.R + col.G + col.B) / 3;
        }

        public string GrayscaleImageToASCIIBasic(Bitmap bmp)
        {
            @string.Clear();

            Color col;

            using (var fb = bmp.FastLock())
            {
                // Loop through each pixel in the bitmap
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        // Get the color of the current pixel
                        //col = bmp.GetPixel(x, y);

                        col = fb.GetPixel(x, y);

                        // To convert to grayscale, the easiest method is to add
                        // the R+G+B colors and divide by three to get the gray
                        // scaled color.
                        // Get the R(ed) value from the grayscale color,
                        // parse to an int. Will be between 0-255.
                        // Append the "color" using various darknesses of ASCII
                        // character.
                        @string.Append(GetGrayShade(((col.R * 30) + (col.G * 59) + (col.B * 11) )/ 100)); 


                    }
                    // If we're at the width, insert a line break
                    @string.Append(Environment.NewLine);
                }
            }

            return @string.ToString();
        }
    }
}
