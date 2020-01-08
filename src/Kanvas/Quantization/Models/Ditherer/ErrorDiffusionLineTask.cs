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

        public IList<ErrorDiffusionElement> Elements { get; }

        public int ProcessedElements { get; private set; }

        public bool IsFinished { get; private set; }

        public ErrorDiffusionLineTask(IList<ErrorDiffusionElement> input, int start, int length, int threshold, ErrorDiffusionLineTask parentTask)
        {
            if (length < threshold)
                throw new InvalidOperationException("Line length can't be smaller than the start threshold.");

            Elements = input;

            _parentTask = parentTask;

            _start = start;
            _length = length;
            _threshold = threshold;
        }

        public void Process(Action<ErrorDiffusionLineTask, int> processDelegate)
        {
            while (ProcessedElements < _length)
            {
                if (!(_parentTask?.IsFinished ?? true) && _parentTask?.ProcessedElements - ProcessedElements < _threshold)
                    continue;

                processDelegate(this, ProcessedElements + _start);

                ProcessedElements++;
            }

            IsFinished = true;
        }
    }
}
