﻿using Kontract.Interfaces.Managers;

namespace Kuriimu2.Wpf.Interfaces
{
    /// <summary>
    /// This is the UI editor interface for simplifying usage of editor controls.
    /// </summary>
    internal interface IFileEditor
    {
        /// <summary>
        /// Provides access to the KoreFile instance associated with the editor.
        /// </summary>
        IStateInfo KoreFile { get; set;  }
    }
}
