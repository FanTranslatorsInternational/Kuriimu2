﻿using Kontract.Extensions;
using Kontract.Models.IO;

namespace Kontract.Interfaces.FileSystem.EventArgs
{
    /// <summary>
    /// Represents a file opening.
    /// </summary>
    public class FileOpenedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Absolute path to the opened file.
        /// </summary>
        public UPath OpenedPath { get; }

        public FileOpenedEventArgs(UPath fullPath)
        {
            fullPath.AssertNotNull(nameof(fullPath));
            fullPath.AssertAbsolute(nameof(fullPath));

            OpenedPath = fullPath;
        }
    }
}
