using System;
using Kontract.Interfaces.Managers.Files;
using Kontract.Models.Managers.Files;

namespace Kore.Models.Managers.Files
{
    public class KoreLoadResult : LoadResult
    {
        /// <summary>
        /// The reason for the cancellation or error.
        /// </summary>
        public LoadErrorReason Reason { get; }

        public static KoreLoadResult Cancel(LoadErrorReason reason) => new KoreLoadResult(LoadStatus.Cancel, reason, null, null);

        protected KoreLoadResult(LoadStatus status, LoadErrorReason reason, IFileState fileState, Exception exception) : base(status, fileState, exception)
        {
            Reason = reason;
        }

        public KoreLoadResult(IFileState fileState) : base(fileState) { }

        public KoreLoadResult(LoadErrorReason reason, Exception exception) : base(exception)
        {
            Reason = reason;
        }

        public KoreLoadResult(LoadErrorReason reason) : base(LoadStatus.Error, null, null)
        {
            Reason = reason;
        }
    }
}
