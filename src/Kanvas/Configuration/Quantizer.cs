using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Quantization.ColorCaches;
using Kontract;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class Quantizer : IQuantizer
    {
        private readonly int _taskCount;

        private readonly IColorQuantizer _colorQuantizer;
        private readonly IColorDitherer _colorDitherer;

        private readonly Func<IList<Color>> _paletteFunc;
        private readonly Func<IList<Color>, IColorCache> _colorCacheFunc;

        public Quantizer(IColorQuantizer colorQuantizer, IColorDitherer colorDitherer, int taskCount,
            Func<IList<Color>> paletteFunc, Func<IList<Color>, IColorCache> colorCacheFunc)
        {
            ContractAssertions.IsNotNull(colorQuantizer, nameof(colorQuantizer));

            _taskCount = taskCount;

            _colorQuantizer = colorQuantizer;
            _colorDitherer = colorDitherer;
            _paletteFunc = paletteFunc;
            _colorCacheFunc = colorCacheFunc;
        }

        /// <inheritdoc />
        public (IEnumerable<int>, IList<Color>) Process(IEnumerable<Color> colors)
        {
            var colorList = colors.ToList();

            var colorCache = GetColorCache(colorList);
            // TODO: Just return edited colors from ditherer to combine index gathering?
            var indices = _colorDitherer?.Process(colorList, colorCache) ??
                          Composition.ComposeIndices(colorList, colorCache, _taskCount);

            return (indices, colorCache.Palette);
        }

        private IColorCache GetColorCache(IEnumerable<Color> colors)
        {
            var palette = _paletteFunc?.Invoke();
            if (palette != null)
                return InvokeColorCacheDelegate(palette);

            palette = _colorQuantizer.CreatePalette(colors);
            return _colorQuantizer.IsColorCacheFixed ?
                _colorQuantizer.GetFixedColorCache(palette) :
                InvokeColorCacheDelegate(palette);
        }

        private IColorCache InvokeColorCacheDelegate(IList<Color> palette)
        {
            return _colorCacheFunc?.Invoke(palette) ??
                   new EuclideanDistanceColorCache(palette);
        }
    }
}
