﻿using FastBitmapLib;
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

        private static char GetGrayShade(int redValue)
        {
            // This is slower than if/else chaining but it's a bit more clear
            // This is still MUCH faster than looping in a collection
            return redValue switch
            {
                var num when num > 230 => '@',
                var num when num > 200 => '#',
                var num when num > 180 => '8',
                var num when num > 160 => '&',
                var num when num > 130 => 'O',
                var num when num > 100 => ':',
                var num when num > 70 => '*',
                var num when num > 50 => '.',
                var num when num > -1 => ' ',
                _ => throw new Exception($"GetGrayShade for value {redValue}"),
            };
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
