using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kore.Batch
{
    public class BatchErrorReport<TErrorObject>
    {
        public TErrorObject ErrorObject { get; }

        public Exception CatchedException { get; }

        public BatchErrorReport(TErrorObject errorObject, Exception exception)
        {
            ErrorObject = errorObject;
            CatchedException = exception;
        }

        public override string ToString()
        {
            var msg = CatchedException.Message;
            msg += $"{Environment.NewLine}{ErrorObject.ToString()}";

            return msg;
        }
    }
}
