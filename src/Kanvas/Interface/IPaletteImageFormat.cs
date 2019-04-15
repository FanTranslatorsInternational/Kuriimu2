using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Models;

namespace Kanvas.Interface
{
    public interface IPaletteImageFormat
    {
        IEnumerable<IndexData> LoadIndeces(byte[] data);

        Color RetrieveColor(IndexData indexData, IList<Color> palette);

        IndexData RetrieveIndex(Color color, IList<Color> palette);

        byte[] SaveIndices(IEnumerable<IndexData> indeces);
    }
}
