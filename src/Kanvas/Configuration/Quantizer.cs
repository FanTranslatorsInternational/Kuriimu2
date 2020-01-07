using System;
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
        private readonly IColorCache _colorCache;

        public Quantizer(IColorQuantizer colorQuantizer, IColorDitherer colorDitherer, IColorCache colorCache)
        {
            ContractAssertions.IsNotNull(colorQuantizer, nameof(colorQuantizer));

            _colorQuantizer = colorQuantizer;
            _colorDitherer = colorDitherer;
            _colorCache = colorCache;
        }

        public (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors)
        {
            var colorList = colors.ToList();

            // TODO: Rethink approach of returning a color cache from the quantizer
            // Main problem denying not returning a color cache is wus color quantizer
            // TODO: Set taskCount correctly
            var colorCache = _colorCache ?? _colorQuantizer.CreateColorCache(colorList);
            var indices = _colorDitherer?.Process(colorList, colorCache) ??
                          Composition.ComposeIndices(colorList, colorCache, Environment.ProcessorCount);

            return (indices, colorCache.Palette);
        }
    }
}
