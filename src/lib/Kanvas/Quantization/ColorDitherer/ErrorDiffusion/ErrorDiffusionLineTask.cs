using Kanvas.DataClasses.Quantization.Ditherer;

namespace Kanvas.Quantization.ColorDitherer.ErrorDiffusion
{
    class ErrorDiffusionLineTask
    {
        private readonly ErrorDiffusionLineTask? _parentTask;
        private readonly IEnumerable<ErrorDiffusionElement> _elements;

        private readonly int _start;
        private readonly int _length;
        private readonly int _threshold;

        public int ProcessedElements { get; private set; }

        public bool IsFinished { get; private set; }

        public ErrorDiffusionLineTask(IEnumerable<ErrorDiffusionElement> input, int start, int length, int threshold, ErrorDiffusionLineTask? parentTask)
        {
            if (length < threshold)
                throw new InvalidOperationException("Line length can't be smaller than the start threshold.");

            _elements = input;

            _parentTask = parentTask;

            _start = start;
            _length = length;
            _threshold = threshold;
        }

        public void Process(Action<ErrorDiffusionElement, int> processDelegate)
        {
            var enumerator = _elements.GetEnumerator();

            while (ProcessedElements < _length)
            {
                if (!(_parentTask?.IsFinished ?? true) && _parentTask?.ProcessedElements - ProcessedElements < _threshold)
                    continue;

                enumerator.MoveNext();

                processDelegate(enumerator.Current, ProcessedElements + _start);

                ProcessedElements++;
            }

            IsFinished = true;
            enumerator.Dispose();
        }
    }
}
