using System;
using System.Collections;
using System.Collections.Generic;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionList<TInput1, TInput2> : IList<ErrorDiffusionElement<TInput1, TInput2>>
    {
        private readonly TInput1[] _indeces;
        private readonly TInput2[] _errors;

        public ErrorDiffusionList(TInput1[] indeces, TInput2[] errors)
        {
            _indeces = indeces;
            _errors = errors;
        }

        public IEnumerator<ErrorDiffusionElement<TInput1, TInput2>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ErrorDiffusionElement<TInput1, TInput2> item)
        {
            throw new NotSupportedException("Read-only collection.");
        }

        public void Clear()
        {
            throw new NotSupportedException("Read-only collection.");
        }

        public bool Contains(ErrorDiffusionElement<TInput1, TInput2> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ErrorDiffusionElement<TInput1, TInput2>[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < array.Length; i++)
                array[i] = new ErrorDiffusionElement<TInput1, TInput2>(_indeces, _errors, i);
        }

        public bool Remove(ErrorDiffusionElement<TInput1, TInput2> item)
        {
            throw new NotSupportedException("Read-only collection.");
        }

        public int Count => _indeces.Length;
        public bool IsReadOnly => true;
        public int IndexOf(ErrorDiffusionElement<TInput1, TInput2> item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ErrorDiffusionElement<TInput1, TInput2> item)
        {
            throw new NotSupportedException("Read-only collection.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Read-only collection.");
        }

        public ErrorDiffusionElement<TInput1, TInput2> this[int index]
        {
            get => new ErrorDiffusionElement<TInput1, TInput2>(_indeces, _errors, index);
            set => throw new NotSupportedException("Read-only collection.");
        }
    }
}
