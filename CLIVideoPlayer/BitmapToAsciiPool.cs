using Microsoft.Extensions.ObjectPool;
using System.IO;

namespace CLIVideoPlayer;

// Yay, no reallocations \:D/
public class BitmapToAsciiHelper : PooledObjectPolicy<BitmapToAscii>
{
    public int CacheDefaultCapacity { get; set; }

    private BitmapToAsciiHelper() { }

    public override BitmapToAscii Create()
    {
        return new BitmapToAscii(new MemoryStream(CacheDefaultCapacity));
    }

    public override bool Return(BitmapToAscii obj)
    {
        // Reset the buffer cursor so we can reuse it as if it were new without allocating new memory

        var buffer = obj.FrameBuffer;

        buffer.SetLength(0);

        return true;
    }

    public static ObjectPool<BitmapToAscii> GetBitmapToAsciiPool(int framebufferSize)
    {
        var poolProvider = new DefaultObjectPoolProvider();

        var settings = new BitmapToAsciiHelper()
        {
            CacheDefaultCapacity = framebufferSize,
        };

        var converters = poolProvider.Create(settings);

        return converters;
    }
}