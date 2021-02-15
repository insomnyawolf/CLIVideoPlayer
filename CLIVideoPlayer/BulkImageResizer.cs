using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CLIVideoPlayer
{
    public class BulkImageResizer
    {
        private Bitmap DestImage;
        private Graphics Graphics;
        private ImageAttributes ImageAttributes;
        private Rectangle destRect;

        // More info on https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
        public BulkImageResizer(Size size, float HorizontalResolution, float VerticalResolution)
        {
            DestImage = new Bitmap(size.Width, size.Height);
            DestImage.SetResolution(HorizontalResolution, VerticalResolution);

            Graphics = Graphics.FromImage(DestImage);
            Graphics.CompositingMode = CompositingMode.SourceCopy;
            Graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Quality
            //Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            // Balance
            //Graphics.InterpolationMode = InterpolationMode.Bilinear;

            // Speed
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            ImageAttributes = new ImageAttributes();
            ImageAttributes.SetWrapMode(WrapMode.TileFlipXY);

            destRect = new Rectangle(0, 0, DestImage.Width, DestImage.Height);
        }

        public Bitmap Resize(Image image)
        {
            Graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ImageAttributes);
            return DestImage;
        }

        public static Bitmap ResizeSingle(Image image, Size size)
        {
            Bitmap destImage = new Bitmap(size.Width, size.Height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
                    var destRect = new Rectangle(0, 0, destImage.Width, destImage.Height);

                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
                }
            }

            return destImage;
        }

        public static Size AspectRatioResizeCalculator(Size origin, Size target)
        {
            var width = origin.Width;
            var height = origin.Height;

            decimal coefficientFitWidth = CoefficientChange(width, target.Width);
            decimal coefficientFitHeight = CoefficientChange(height, target.Height);

            decimal coefficient = coefficientFitWidth < coefficientFitHeight ? coefficientFitWidth : coefficientFitHeight;

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
