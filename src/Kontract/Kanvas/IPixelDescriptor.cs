using System.Drawing;

namespace Kontract.Kanvas
{
    public interface IPixelDescriptor
    {
        string GetPixelName();

        int GetBitDepth();

        Color GetColor(long value);

        long GetValue(Color color);
    }
}
