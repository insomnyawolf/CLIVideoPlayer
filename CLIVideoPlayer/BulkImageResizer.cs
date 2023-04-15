﻿using Microsoft.Extensions.ObjectPool;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CLIVideoPlayer
{
    public enum ResizerQuality
    {
        HighQuality,
        Balanced,
        HighSpeed
    }

    public class BulkImageResizerSettings
    {
        public Size Size { get; set; }
        public float HorizontalResolution { get; set; }
        public float VerticalResolution { get; set; }
        public ResizerQuality ResizerQuality { get; set; }
    }

    public class BulkImageResizer
    {
        private Bitmap DestImage;
        private Graphics Graphics;
        private ImageAttributes ImageAttributes;
        private Rectangle destRect;

        // More info on https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
        public BulkImageResizer(BulkImageResizerSettings BulkImageResizerSettings)
        {
            DestImage = new Bitmap(BulkImageResizerSettings.Size.Width, BulkImageResizerSettings.Size.Height);
            DestImage.SetResolution(BulkImageResizerSettings.HorizontalResolution, BulkImageResizerSettings.VerticalResolution);

            Graphics = Graphics.FromImage(DestImage);

            Graphics.CompositingMode = CompositingMode.SourceCopy;

            switch (BulkImageResizerSettings.ResizerQuality)
            {
                case ResizerQuality.HighQuality:
                    Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    break;
                case ResizerQuality.Balanced:
                    Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    Graphics.InterpolationMode = InterpolationMode.Bilinear;
                    Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    break;
                case ResizerQuality.HighSpeed:
                    Graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    Graphics.SmoothingMode = SmoothingMode.None;
                    break;

            }

            ImageAttributes = new ImageAttributes();
            ImageAttributes.SetWrapMode(WrapMode.TileFlipXY);

            destRect = new Rectangle(0, 0, DestImage.Width, DestImage.Height);
        }

        public Bitmap Resize(Image image)
        {
            Graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ImageAttributes);
            return DestImage;
        }

        public static Size AspectRatioResizeCalculator(Size origin, Size target)
        {
            var width = origin.Width;
            var height = origin.Height;

            decimal coefficientFitWidth = CoefficientChange(width, target.Width);
            decimal coefficientFitHeight = CoefficientChange(height, target.Height);

            decimal coefficient = coefficientFitWidth < coefficientFitHeight ? coefficientFitWidth : coefficientFitHeight;

            // Avoid Upscaling
            if (coefficient > 1)
            {
                return origin;
            }

            width = decimal.ToInt32(coefficient * origin.Width);
            height = decimal.ToInt32(coefficient * origin.Height);

            // Images must have at least 1 px on both sides
            // This fixes it
            coefficient = 0;
            if (width < 1)
            {
                coefficient = CoefficientChange(width, 1);
            }
            else if (height < 1)
            {
                coefficient = CoefficientChange(height, 1);
            }

            if (coefficient != 0)
            {
                height = decimal.ToInt32(coefficient * origin.Width);
                width = decimal.ToInt32(coefficient * origin.Height);
            }

            return new Size
            {
                Width = width,
                Height = height,
            };
        }

        private static decimal CoefficientChange(int valorInicial, int valorFinal)
        {
            return 100M / valorInicial * valorFinal / 100;
        }
    }
}
