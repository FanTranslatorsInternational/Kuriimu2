using System;
using Kontract.Interfaces.Managers;

namespace Kontract.Models
{
    public class LoadResult
    {
        /// <summary>
        /// Determines the status of the load.
        /// </summary>
        private LoadStatus Status { get; }
        
        /// <summary>
        /// Declares if the load process was successful.
        /// </summary>
        public bool IsSuccessful => Status == LoadStatus.Successful;
        
        /// <summary>
        /// Declares if the load process was cancelled.
        /// </summary>
        public bool IsCancelled => Status == LoadStatus.Cancelled;

        /// <summary>
        /// Contains the result if the load process was successful.
        /// </summary>
        public IFileState LoadedFileState { get; }

        /// <summary>
        /// Contains a human readable message about the state of the save process.
        /// </summary>
        /// <remarks>Can contain a message, even if the save process was successful. <see langword="null" /> otherwise.</remarks>
        public string Message { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        private LoadResult(LoadStatus status)
        {
            Status = status;
        }
        
        public static LoadResult CancelledResult => new LoadResult(LoadStatus.Cancelled);

        public LoadResult(IFileState fileState) :
            this(LoadStatus.Successful)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));

            LoadedFileState = fileState;
        }

        public LoadResult(bool isSuccessful)
        {
            Status = isSuccessful ? LoadStatus.Successful : LoadStatus.Errored;
        }

        public LoadResult(bool isSuccessful, string message) :
            this(isSuccessful)
        {
            Message = message;
        }

        public LoadResult(Exception exception) :
            this(LoadStatus.Errored)
        {
            ContractAssertions.IsNotNull(exception, nameof(exception));

            Message = exception.Message;
            Exception = exception;
        }
        
    }

    public enum LoadStatus
    {
        Successful,
        Cancelled,
        Errored
    }
}
