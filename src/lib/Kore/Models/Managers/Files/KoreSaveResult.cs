using System;
using Kontract.Models.Managers.Files;

namespace Kore.Models.Managers.Files
{
    public class KoreSaveResult : SaveResult
    {
        /// <summary>
        /// The reason for the error.
        /// </summary>
        public SaveErrorReason Reason { get; }

        public static KoreSaveResult Success => new KoreSaveResult(SaveErrorReason.None, true, null);

        protected KoreSaveResult(SaveErrorReason reason, bool isSuccessful, Exception exception) :
            base(isSuccessful, exception)
        {
            Reason = reason;
        }

        public KoreSaveResult(SaveErrorReason reason) : this(reason, false, null)
        { }

        public KoreSaveResult(SaveErrorReason reason, Exception exception) : this(reason, false, exception)
        { }
    }
}
