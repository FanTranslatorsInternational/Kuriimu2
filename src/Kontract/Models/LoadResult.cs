using System;
using Kontract.Interfaces.Managers;

namespace Kontract.Models
{
    public class LoadResult
    {
        /// <summary>
        /// Declares if the load was successful, unsuccessful or canceled (null).
        /// Use IsSuccessful, IsUnsuccessful and IsCanceled.
        /// </summary>
        private bool? Status { get; }

        /// <summary>
        /// Declares if the load process was successful.
        /// </summary>
        public virtual bool IsSuccessful => Status == true;
        
        /// <summary>
        /// Declares if the load process was unsuccessful due to an error.
        /// </summary>
        public virtual bool IsUnsuccessful => Status == false;
        
        /// <summary>
        /// Declares if the load process was canceled.
        /// </summary>
        public virtual bool IsCanceled => Status == null;

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

        public LoadResult(bool? status)
        {
            Status = status;
        }

        public LoadResult(bool? status, string message) :
            this(status)
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
