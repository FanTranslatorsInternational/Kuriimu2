using System;
using System.Collections.Generic;

namespace Kontract.Models.Managers.Files
{
    public class SaveStreamResult : SaveResult
    {
        /// <summary>
        /// The list of in-memory files, that were saved by the operation.
        /// </summary>
        public IReadOnlyList<StreamFile> SavedStreams { get; }

        protected SaveStreamResult(IReadOnlyList<StreamFile> files, bool isSuccessful, Exception exception) :
            base(isSuccessful, exception)
        {
            SavedStreams = files;
        }

        public SaveStreamResult(IReadOnlyList<StreamFile> files) : this(files, true, null)
        { }

        public SaveStreamResult(Exception exception) : this(Array.Empty<StreamFile>(), false, exception)
        { }
    }
}
