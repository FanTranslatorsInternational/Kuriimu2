using System;
using Kontract.Interfaces.Managers.Files;

namespace Kontract.Models.Managers.Files
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
        public bool IsSuccessful => Status == LoadStatus.Success;

        /// <summary>
        /// Declares if the load process was cancelled.
        /// </summary>
        public bool IsCancelled => Status == LoadStatus.Cancel;

        /// <summary>
        /// Contains the result if the load process was successful.
        /// </summary>
        public IFileState LoadedFileState { get; }

        /// <summary>
        /// Contains an exception, if any subsequent process was unsuccessful and finished with an exception.
        /// </summary>
        public Exception Exception { get; }

        protected LoadResult(LoadStatus status, IFileState fileState, Exception exception)
        {
            Status = status;
            LoadedFileState = fileState;
            Exception = exception;
        }

        public LoadResult(IFileState fileState)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));

            Status = LoadStatus.Success;
            LoadedFileState = fileState;
        }

        public LoadResult(Exception exception)
        {
            ContractAssertions.IsNotNull(exception, nameof(exception));

            Status = LoadStatus.Error;
            Exception = exception;
        }
    }

    public enum LoadStatus
    {
        Success,
        Cancel,
        Error
    }
}
