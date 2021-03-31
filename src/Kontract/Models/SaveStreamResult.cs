using System;
using System.Collections.Generic;

namespace Kontract.Models
{
    public class SaveStreamResult : SaveResult
    {
        /// <summary>
        /// The list of in-memory files, that were saved by the operation.
        /// </summary>
        public IReadOnlyList<StreamFile> SavedStream { get; } = new List<StreamFile>();

        public SaveStreamResult(IReadOnlyList<StreamFile> files) : base(true, "")
        {
            SavedStream = files;
        }

        public SaveStreamResult(bool isSuccessful, string message) : base(isSuccessful, message)
        {
        }

        public SaveStreamResult(Exception exception) : base(exception)
        {
        }
    }
}
