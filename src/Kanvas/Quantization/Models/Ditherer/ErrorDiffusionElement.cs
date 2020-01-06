using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionElement<TInput1, TInput2>
    {
        private readonly IList<TInput1> _input;
        private readonly TInput2[] _errors;
        private readonly int _index;

        public TInput1 Input
        {
            get => _input[_index];
            set => _input[_index] = value;
        }

        public TInput2 Error
        {
            get => _errors[_index];
            set => _errors[_index] = value;
        }

        public ErrorDiffusionElement(IList<TInput1> input, TInput2[] errors, int index)
        {
            _input = input;
            _errors = errors;
            _index = index;
        }
    }
}
