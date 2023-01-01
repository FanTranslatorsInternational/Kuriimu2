using System;

namespace Kontract.Models.Managers.Files
{
    public class CloseResult
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
        public static readonly CloseResult SuccessfulResult = new CloseResult(true);

        public CloseResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }

        public CloseResult(bool isSuccessful, string message) :
            this(isSuccessful)
        {
            Message = message;
        }

        public CloseResult(Exception exception) :
            this(false)
        {
            ContractAssertions.IsNotNull(exception, nameof(exception));

            Message = exception.Message;
            Exception = exception;
        }
    }
}
