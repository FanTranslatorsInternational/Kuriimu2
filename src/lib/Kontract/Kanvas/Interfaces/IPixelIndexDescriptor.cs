using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas.Interfaces
{
    public interface IPixelIndexDescriptor
    {
        string GetPixelName();

        int GetBitDepth();

        Color GetColor(long value, IList<Color> palette);

        long GetValue(int index, IList<Color> palette);
    }
}
