using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
