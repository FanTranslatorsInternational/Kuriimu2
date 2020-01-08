using System.Collections.Generic;
using System.Drawing;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionElement
    {
        private readonly IList<Color> _colors;
        private readonly IList<ColorComponentError> _errors;
        private readonly IList<int> _indices;
        private readonly int _index;

        public Color Input
        {
            get => _colors[_index];
            set => _colors[_index] = value;
        }

        public ColorComponentError Error
        {
            get => _errors[_index];
            set => _errors[_index] = value;
        }

        public int PaletteIndex
        {
            get => _indices[_index];
            set => _indices[_index] = value;
        }

        public ErrorDiffusionElement(IList<Color> colors, IList<ColorComponentError> errors, IList<int> indices, int index)
        {
            _colors = colors;
            _errors = errors;
            _indices = indices;
            _index = index;
        }
    }
}
