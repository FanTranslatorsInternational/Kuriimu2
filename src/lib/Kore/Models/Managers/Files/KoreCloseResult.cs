using System;
using Kontract.Models.Managers.Files;

namespace Kore.Models.Managers.Files
{
    public class KoreCloseResult : CloseResult
    {
        /// <summary>
        /// The reason for the error.
        /// </summary>
        public CloseErrorReason Reason { get; }

        public static KoreCloseResult Success => new KoreCloseResult(CloseErrorReason.None, true, null);

        protected KoreCloseResult(CloseErrorReason reason, bool isSuccessful, Exception exception) : base(isSuccessful, exception)
        {
            Reason = reason;
        }

        public KoreCloseResult(CloseErrorReason reason) : this(reason, false, null)
        { }

        public KoreCloseResult(CloseErrorReason reason, Exception exception) : this(reason, false, exception)
        { }
    }
}
