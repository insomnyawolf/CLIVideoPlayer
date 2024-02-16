using FFMediaToolkit.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace CLIVideoPlayer;

internal class ImageSarpHelpers
{
    public static ResizeProcessor GetResizeProcessor(Size sourceSize, Size targetSize)
    {
        var options = new ResizeOptions
        {
            Size = targetSize,
            Mode = ResizeMode.Manual,
            Sampler = KnownResamplers.NearestNeighbor,
            TargetRectangle = new Rectangle(Point.Empty, targetSize),
            Compand = false,
        };

        var resizeProcessor = new ResizeProcessor(options, sourceSize);

        return resizeProcessor;
    }

    private static readonly Configuration Configuration = new()
    {
        //PreferContiguousImageBuffers = true,
    };

    public static Image<Bgr24> ToImage(ImageData imageData)
    {
        return Image.LoadPixelData<Bgr24>(Configuration, imageData.Data, imageData.ImageSize.Width, imageData.ImageSize.Height);
    }
}
