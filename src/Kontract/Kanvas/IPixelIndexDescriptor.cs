using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Kanvas
{
    public interface IPixelIndexDescriptor
    {
        string GetPixelName();

        int GetBitDepth();

        Color GetColor(long value, IList<Color> palette);

        long GetValue(int index, IList<Color> palette);
    }
}
