using Microsoft.Extensions.ObjectPool;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace CLIVideoPlayer
{
    public class ImageConverterPooledObjectPolicy : PooledObjectPolicy<ImageConverter>
    {
        public BulkImageResizerSettings BulkImageResizerSettings { get; set; }
        public override ImageConverter Create()
        {
            return new ImageConverter(BulkImageResizerSettings);
        }

        public override bool Return(ImageConverter obj)
        {
            // Reset the buffer cursor so we can reuseit as if it were new without allocating new memory
            obj.FrameBuffer.Position = 0;

            return true;
        }
    }

    // This is basically a cached class, nothing here is "important"
    // but since we have allocated memory for it already we can reuse it
    public class ImageConverter
    {
        public BulkImageResizer BulkImageResizer { get; set; }
        public MemoryStream FrameBuffer { get; set; }

        public ImageConverter(BulkImageResizerSettings BulkImageResizerSettings)
        {
            FrameBuffer = new MemoryStream();

            BulkImageResizer = new BulkImageResizer(BulkImageResizerSettings);
        }

        public async Task Convert(Bitmap Raw)
        {
            var resized = BulkImageResizer.Resize(Raw);

            await BitmapToAscii.UpdateFrameBuffer(resized, FrameBuffer);
        }
    }
}
