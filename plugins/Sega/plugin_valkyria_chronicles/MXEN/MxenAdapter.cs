using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_valkyria_chronicles.MXEN
{
    [Export(typeof(MxenAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("66812C1F-BDB6-44A5-819D-4FAD9B991A65", "VC-MXEN Data", "MXEN", "IcySon55", "", "This is the MXE text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mxe")]
    public sealed class MxenAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private MXEN _format;

        #region Properties

        public IEnumerable<TextEntry> Entries => _format?.Entries;

        public string NameFilter => @".*";

        public int NameMaxLength => 0;

        public string LineEndings { get; set; } = "\n";

        public bool LeaveOpen { get; set; }

        #endregion

        public bool Identify(StreamInfo input)
        {
            try
            {
                using (var br = new BinaryReaderX(input.FileData, true))
                    return br.PeekString() == "MXEN";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new MXEN(input.FileData);
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
