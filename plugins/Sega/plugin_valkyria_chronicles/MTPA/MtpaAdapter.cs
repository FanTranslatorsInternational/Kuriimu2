using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_valkyria_chronicles.MTPA
{
    [Export(typeof(MtpaAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("FD00E783-0904-4A9E-8575-59CDA5A165B9", "VC-MTPA Text", "MTPA", "IcySon55", "", "This is the MTP text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mtp")]
    public sealed class MtpaAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private MTPA _format;

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
                    return br.PeekString() == "MTPA";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new MTPA(input.FileData);
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
