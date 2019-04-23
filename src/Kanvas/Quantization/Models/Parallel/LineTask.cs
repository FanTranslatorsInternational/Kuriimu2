using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models.Parallel
{
    internal class LineTask<TInput, TOutput>
    {
        public TInput Input { get; }
        public TOutput Output { get; }
        public int Start { get; }
        public int Length { get; }

        public LineTask(TInput input, TOutput output, int start, int length)
        {
            Input = input;
            Output = output;
            Start = start;
            Length = length;
        }
    }
}
