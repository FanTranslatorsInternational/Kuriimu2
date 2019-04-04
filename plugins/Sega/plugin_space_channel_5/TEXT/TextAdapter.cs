using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_space_channel_5.TEXT
{
    [Export(typeof(TextAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("46944E91-2F9C-4082-B0A2-503C5DC8824D", "SC5-TEXT Text", "TEXT", "IcySon55", "", "This is the Space Channel 5 text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.bin")]
    public sealed class TextAdapter : ITextAdapter, IIdentifyFiles, ICreateFiles, ILoadFiles, ISaveFiles
    {
        private TEXT _format;

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
                {
                    var magic = br.ReadString(4);
                    var fileSize = br.ReadInt32();
                    return magic == "TEXT" && fileSize == br.BaseStream.Length;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Create()
        {
            _format = new TEXT();
        }

        public void Load(StreamInfo input)
        {
            _format = new TEXT(input.FileData);
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
