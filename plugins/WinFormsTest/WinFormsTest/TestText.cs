using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Text;

namespace WinFormsTest
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.text")]
    [PluginInfo("Test-Text-Id")]
    public class TestTextPlugin : ITextAdapter, ILoadFiles, IIdentifyFiles, IMultipleFiles
    {
        public IEnumerable<TextEntry> Entries => _texts;

        private IEnumerable<TextEntry> _texts;

        public string NameFilter => throw new NotImplementedException();

        public int NameMaxLength => throw new NotImplementedException();

        public string LineEndings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IFileSystem FileSystem { get; set; }

        public bool LeaveOpen { get; set; }

        public void Dispose()
        {
            ;
        }

        public bool Identify(StreamInfo file)
        {
            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, LeaveOpen))
            {
                return br.ReadUInt32() == 0x16161616;
            }
        }

        public void Load(StreamInfo file)
        {
            // Here a format class can get initialized and all opened files passed in
            var buffer = new byte[5];
            file.FileData.Read(buffer, 0, 5);
            _texts = new List<TextEntry> { new TextEntry { Name = "First entry", OriginalText = Encoding.ASCII.GetString(buffer) } };
        }
    }
}
