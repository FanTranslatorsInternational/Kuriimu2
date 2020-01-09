using System;
using System.Collections.Generic;

namespace Kanvas.Quantization.Models.Ditherer
{
    class ErrorDiffusionLineTask
    {
        private readonly ErrorDiffusionLineTask _parentTask;

        private readonly int _start;
        private readonly int _length;
        private readonly int _threshold;

        private IEnumerable<ErrorDiffusionElement2> _elements;

        public int ProcessedElements { get; private set; }

        public bool IsFinished { get; private set; }

        public ErrorDiffusionLineTask(IEnumerable<ErrorDiffusionElement2> input, int start, int length, int threshold, ErrorDiffusionLineTask parentTask)
        {
            if (length < threshold)
                throw new InvalidOperationException("Line length can't be smaller than the start threshold.");

            _elements = input;

            _parentTask = parentTask;

            _start = start;
            _length = length;
            _threshold = threshold;
        }

        public void Process(Action<ErrorDiffusionElement2, int> processDelegate)
        {
            var enumerator = _elements.GetEnumerator();

            while (ProcessedElements < _length)
            {
                if (!(_parentTask?.IsFinished ?? true) && _parentTask?.ProcessedElements - ProcessedElements < _threshold)
                    continue;

                enumerator.MoveNext();

                try
                {
                    processDelegate(enumerator.Current, ProcessedElements + _start);
                }
                catch (Exception e)
                {
                    ;
                }

                ProcessedElements++;
            }

            IsFinished = true;
            enumerator.Dispose();
        }
    }
}
