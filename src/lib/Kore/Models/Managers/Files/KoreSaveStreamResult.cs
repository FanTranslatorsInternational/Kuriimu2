using System;
using System.Collections.Generic;
using Kontract.Models.Managers.Files;

namespace Kore.Models.Managers.Files
{
    public class KoreSaveStreamResult : SaveStreamResult
    {
        /// <summary>
        /// The reason for the error.
        /// </summary>
        public SaveErrorReason Reason { get; }

        protected KoreSaveStreamResult(SaveErrorReason reason, IReadOnlyList<StreamFile> files, bool isSuccessful, Exception exception) :
            base(files, isSuccessful, exception)
        {
            Reason = reason;
        }

        public KoreSaveStreamResult(IReadOnlyList<StreamFile> files) : this(SaveErrorReason.None, files, true, null)
        { }

        public KoreSaveStreamResult(SaveErrorReason reason) : this(reason, Array.Empty<StreamFile>(), false, null)
        { }

        public KoreSaveStreamResult(SaveErrorReason reason, Exception exception) : this(reason, Array.Empty<StreamFile>(), false, exception)
        { }
    }
}
