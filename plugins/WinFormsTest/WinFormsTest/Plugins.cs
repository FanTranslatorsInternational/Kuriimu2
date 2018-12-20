using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsTest
{
    [Export(typeof(ITextAdapter))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(IIdentifyFiles))]
    [PluginExtensionInfo("*.text")]
    [PluginInfo("Test-Text-Id")]
    public class TestTextPlugin : ITextAdapter, ILoadFiles, IIdentifyFiles
    {
        public IEnumerable<TextEntry> Entries => _texts.Select(x => new TextEntry { OriginalText = x });

        private IEnumerable<string> _texts;

        public string NameFilter => throw new NotImplementedException();

        public int NameMaxLength => throw new NotImplementedException();

        public string LineEndings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int MinimumRequiredFiles => 1;

        public void Dispose()
        {
            ;
        }

        public bool Identify(string filename)
        {
            using (var br = new BinaryReader(File.OpenRead(filename)))
            {
                return br.ReadUInt32() == 0x16161616;
            }
        }

        public void Load(params StreamInfo[] filename)
        {
            _texts = new List<string> { "Text1", "Text2", "Text3" };
        }
    }
}
