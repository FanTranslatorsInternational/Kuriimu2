using System;

namespace Kontract.Models.Managers.Files
{
    public class SaveResult
    {
        /// <summary>
        /// Declares if the save process was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        protected SaveResult(bool isSuccessful, Exception exception)
        {
            IsSuccessful = isSuccessful;
            Exception = exception;
        }

        public SaveResult(bool isSuccessful) : this(isSuccessful, null)
        {
        }

        public SaveResult(Exception exception) : this(false, exception)
        {
        }
    }
}
