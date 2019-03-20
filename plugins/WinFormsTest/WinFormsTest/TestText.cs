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
        public IEnumerable<TextEntry> Entries => _texts.Select(x => new TextEntry { OriginalText = x });

        private IEnumerable<string> _texts;

        public event EventHandler<RequestFileEventArgs> RequestFiles;

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
            using (var br = new BinaryReader(file.FileData))
            {
                return br.ReadUInt32() == 0x16161616;
            }
        }

        private List<StreamInfo> _files;

        public void Load(StreamInfo file)
        {
            // Maybe open more files if needed by the format
            var args = new RequestFileEventArgs { FilePathPattern = "*.text2" };
            RequestFiles(this, args);

            _files = new List<StreamInfo> { file };
            _files.AddRange(args.OpenedStreamInfos);

            // Here a format class can get initialized and all opened files passed in
            var buffer = new byte[5];
            _files[1].FileData.Read(buffer, 0, 5);
            _texts = new List<string> { "Text1", "Text2", "Text3", Encoding.ASCII.GetString(buffer) };
        }
    }
}
