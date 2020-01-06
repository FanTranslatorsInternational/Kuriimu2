using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models.Parallel
{
    class DelayedLineTask<TInput, TOutput>
    {
        private readonly DelayedLineTask<TInput, TOutput> _preLineTask;

        public Exception TaskException { get; private set; }

        public IList<TInput> Input { get; }
        public TOutput Output { get; }
        public int Start { get; }
        public int Length { get; }
        public int StartThreshold { get; }
        public int ProcessedElements { get; private set; }

        public DelayedLineTask(IList<TInput> input, TOutput output, int start, int length, int startThreshold, DelayedLineTask<TInput, TOutput> preLineTask)
        {
            if (length < startThreshold)
                throw new InvalidOperationException("Line length can't be smaller than the start threshold.");

            _preLineTask = preLineTask;

            Input = input;
            Output = output;
            Start = start;
            Length = length;
            StartThreshold = startThreshold;
        }

        public void Process(Action<DelayedLineTask<TInput, TOutput>, int> processElement)
        {
            while (ProcessedElements < Length)
            {
                if (_preLineTask?.ProcessedElements < _preLineTask?.Length && _preLineTask?.ProcessedElements - ProcessedElements < StartThreshold)
                    continue;

                try
                {
                    processElement(this, ProcessedElements + Start);
                }
                catch (Exception ex)
                {
#if DEBUG
                    //Trace.WriteLine(ex.Message);
#endif
                    TaskException = ex;
                    break;
                }

                ProcessedElements++;
            }

            ProcessedElements = Length;
        }
    }
}
