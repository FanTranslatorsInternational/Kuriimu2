using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class Quantizer : IQuantizer
    {
        private readonly IColorQuantizer _colorQuantizer;
        private readonly IColorDitherer _colorDitherer;

        public Quantizer(IColorQuantizer colorQuantizer, IColorDitherer colorDitherer)
        {
            ContractAssertions.IsNotNull(colorQuantizer, nameof(colorQuantizer));

            _colorQuantizer = colorQuantizer;
            _colorDitherer = colorDitherer;
        }

        public (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors)
        {
            if (_colorDitherer != null)
            {
                // TODO: Think about not using a color list, but an enumerable
                var colorList = colors.ToList();

                var palette = _colorQuantizer.CreatePalette(colorList);
                var indices = _colorDitherer.Process(colorList, _colorQuantizer.ColorCache);

                colorList.Clear();
                return (indices, palette);
            }
            else
            {
                var indices = _colorQuantizer.Process(colors).ToArray();
                var palette = _colorQuantizer.ColorCache.Palette;

                return (indices, palette);
            }
        }
    }
}
