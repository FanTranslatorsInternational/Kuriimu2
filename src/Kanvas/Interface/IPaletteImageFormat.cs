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
        /// <summary>
        /// The number of bits one pixel takes in the format definition. Also known as bits per pixel (bpp)
        /// </summary>
        int IndexDepth { get; }

        /// <summary>
        /// The name to display for this format
        /// </summary>
        string FormatName { get; }

        IEnumerable<IndexData> LoadIndeces(byte[] data);

        Color RetrieveColor(IndexData indexData, IList<Color> palette);

        IndexData RetrieveIndex(Color color, IList<Color> palette);

        byte[] SaveIndices(IEnumerable<IndexData> indeces);
    }
}
