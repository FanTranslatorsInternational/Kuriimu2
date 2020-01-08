using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionList : IList<ErrorDiffusionElement>
    {
        private readonly IList<Color> _colors;
        private readonly IList<ColorComponentError> _errors;
        private readonly IList<int> _indices;

        public int Count => _colors.Count;

        public bool IsReadOnly => true;

        public ErrorDiffusionElement this[int index]
        {
            get => new ErrorDiffusionElement(_colors, _errors, _indices, index);
            set => throw new NotSupportedException();
        }

        public ErrorDiffusionList(IList<Color> colors, IList<ColorComponentError> errors, IList<int> indices)
        {
            _colors = colors;
            _errors = errors;
            _indices = indices;
        }

        public IEnumerator<ErrorDiffusionElement> GetEnumerator()
        {
            return _colors.Select((c, i) => new ErrorDiffusionElement(_colors, _errors, _indices, i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ErrorDiffusionElement item)
        {
            _colors.Add(item.Input);
            _errors.Add(item.Error);
        }

        public void Clear()
        {
            _colors.Clear();
            _errors.Clear();
        }

        public bool Contains(ErrorDiffusionElement item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ErrorDiffusionElement[] array, int arrayIndex)
        {
            for (var i = arrayIndex; i < array.Length; i++)
                array[i] = this[i];
        }

        public bool Remove(ErrorDiffusionElement item)
        {
            throw new NotSupportedException();
        }

        public int IndexOf(ErrorDiffusionElement item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, ErrorDiffusionElement item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}
