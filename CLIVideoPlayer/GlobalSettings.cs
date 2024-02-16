using System.Text;

namespace CLIVideoPlayer;

public class GlobalSettings
{
    // By using ascii we improve the performance a lot, we can still represent every color but we only need to write half of the bytes
    // Also since ascii has a fixed size, we can calculate the buffers and allocate them in the required size directly
    public static readonly Encoding Encoding = Encoding.ASCII;
    //public static readonly Encoding Encoding = Encoding.Unicode;
}
