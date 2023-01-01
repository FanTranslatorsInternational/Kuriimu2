using System.Drawing;

namespace Kontract.Kanvas.Interfaces
{
    public interface IPixelDescriptor
    {
        string GetPixelName();

        int GetBitDepth();

        Color GetColor(long value);

        long GetValue(Color color);
    }
}
