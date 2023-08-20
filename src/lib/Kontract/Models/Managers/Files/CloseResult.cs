using System;

namespace Kontract.Models.Managers.Files
{
    public class CloseResult
    {
        /// <summary>
        /// Declares if the close process was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        protected CloseResult(bool isSuccessful, Exception exception)
        {
            IsSuccessful = isSuccessful;
            Exception = exception;
        }

        public CloseResult() : this(true, null)
        {
        }

        public CloseResult(Exception exception) : this(false, exception)
        {
        }
    }
}
