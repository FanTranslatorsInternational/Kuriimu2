using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_valkyria_chronicles.MTPA
{
    [Export(typeof(MtpaAdapter))]
    [Export(typeof(ITextAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("FD00E783-0904-4A9E-8575-59CDA5A165B9", "MTPA Text File", "MTPA", "IcySon55", "", "This is the MTP text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mtp")]
    public sealed class MtpaAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private MTPA _mtpa;

        #region Properties

        public IEnumerable<TextEntry> Entries => _mtpa?.Entries;

        public string NameFilter => @".*";
        public int NameMaxLength => 0;

        public string LineEndings
        {
            get => "\n";
            set => throw new NotImplementedException();
        }

        #endregion

        public bool Identify(string filename)
        {
            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString() == "MTPA";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
                _mtpa = new MTPA(File.OpenRead(filename));
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _mtpa.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
