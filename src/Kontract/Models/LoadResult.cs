using System;
using Kontract.Interfaces.Managers;

namespace Kontract.Models
{
    public class LoadResult
    {
        /// <summary>
        /// Declares if the save process was successful.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Contains the result if the load process was successful.
        /// </summary>
        public IStateInfo LoadedState { get; }

        /// <summary>
        /// Contains a human readable message about the state of the save process.
        /// </summary>
        /// <remarks>Can contain a message, even if the save process was successful. <see langword="null" /> otherwise.</remarks>
        public string Message { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        public LoadResult(IStateInfo stateInfo) :
            this(true)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            LoadedState = stateInfo;
        }

        public LoadResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }

        public LoadResult(bool isSuccessful, string message) :
            this(isSuccessful)
        {
            Message = message;
        }

        public LoadResult(Exception exception) :
            this(false)
        {
            ContractAssertions.IsNotNull(exception, nameof(exception));

            Message = exception.Message;
            Exception = exception;
        }
    }
}
