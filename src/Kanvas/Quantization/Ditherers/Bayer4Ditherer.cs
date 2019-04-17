using Kanvas.Quantization.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Ditherers
{
    public class Bayer4Ditherer : IColorDitherer
    {
        private IColorQuantizer _quantizer;

        public Bayer4Ditherer(IColorQuantizer quantizer)
        {
            _quantizer = quantizer;
        }

        public (IEnumerable<int> indeces, IList<Color> palette) Process(Bitmap image)
        {
            var indeces = new List<int>();
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                    indeces.Add(_quantizer.GetPaletteIndex(image.GetPixel(x, y), x, y));

            return (indeces, _quantizer.GetPalette());
        }
    }
}
