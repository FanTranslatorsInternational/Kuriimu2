using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_valkyria_chronicles.MXEN
{
    [Export(typeof(MxenAdapter))]
    [Export(typeof(ITextAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("66812C1F-BDB6-44A5-819D-4FAD9B991A65", "VC-MXEN Data", "MXEN", "IcySon55", "", "This is the MXE text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.mxe")]
    public sealed class MxenAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private MXEN _mxen;

        #region Properties

        public IEnumerable<TextEntry> Entries => _mxen?.Entries;

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
                    return br.PeekString() == "MXEN";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
                _mxen = new MXEN(File.OpenRead(filename));
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _mxen.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
