using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Batch
{
    public class BatchErrorReport
    {
        public ProcessElement ProcessElement { get; }

        public Exception CatchedException { get; }

        public BatchErrorReport(ProcessElement processElement, Exception exception)
        {
            ProcessElement = processElement;
            CatchedException = exception;
        }

        public override string ToString()
        {
            var msg = CatchedException.Message;
            msg += $"{Environment.NewLine}Input File: {ProcessElement.InputFilename}";
            msg += $"{Environment.NewLine}Output File: {ProcessElement.OutputFilename}";

            return msg;
        }
    }
}
