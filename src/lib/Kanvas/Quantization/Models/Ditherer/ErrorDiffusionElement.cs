using System.Collections.Generic;
using System.Drawing;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionElement
    {
        private readonly IList<Color> _colors;
        private readonly int _colorIndex;

        public Color Color => _colors[_colorIndex];

        public IDictionary<int, ColorComponentError> Errors { get; }

        public IList<int> Indices { get; }

        public ErrorDiffusionElement(IList<Color> colors, int colorIndex, IDictionary<int, ColorComponentError> errors, IList<int> indices)
        {
            _colors = colors;
            _colorIndex = colorIndex;

            Errors = errors;
            Indices = indices;
        }
    }
}
