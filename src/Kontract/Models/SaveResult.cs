using System;

namespace Kontract.Models
{
    public class SaveResult
    {
        /// <summary>
        /// Declares if the save process was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Contains a human readable message about the state of the save process.
        /// </summary>
        /// <remarks>Can contain a message, even if the save process was successful. <see langword="null" /> otherwise.</remarks>
        public string Message { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Returns a successful save result.
        /// </summary>
        public static readonly SaveResult SuccessfulResult = new SaveResult(true);

        private SaveResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }

        public SaveResult(bool isSuccessful, string message) :
            this(isSuccessful)
        {
            Message = message;
        }

        public SaveResult(Exception exception) :
            this(false)
        {
            Message = exception.Message;
            Exception = exception;
        }
    }
}
