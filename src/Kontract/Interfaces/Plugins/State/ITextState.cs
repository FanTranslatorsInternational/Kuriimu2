﻿using System.Collections.Generic;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State
{
    public interface ITextState : IPluginState
    {
        IList<TextEntry> Texts { get; }
        
        #region Optional feature checks
        
        public bool CanAddEntry => this is IAddEntries;
        public bool CanDeleteEntry => this is IDeleteEntries;
        public bool CanRenameEntry => this is IRenameEntries;
        public bool CanShowEntryProperties => this is ITextEntriesHaveExtendedProperties;
        
        #endregion
    }
}
