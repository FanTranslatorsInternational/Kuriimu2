using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_space_channel_5.TEXT
{
    [Export(typeof(TextAdapter))]
    [Export(typeof(ITextAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ICreateFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("46944E91-2F9C-4082-B0A2-503C5DC8824D", "SC5-TEXT Text", "TEXT", "IcySon55", "", "This is the Space Channel 5 text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.bin")]
    public sealed class TextAdapter : ITextAdapter, IIdentifyFiles, ICreateFiles, ILoadFiles, ISaveFiles
    {
        private TEXT _format;

        #region Properties

        public IEnumerable<TextEntry> Entries => _format?.Entries;

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

        public void Load(string filename)
        {
            if (File.Exists(filename))
                _format = new TEXT(File.OpenRead(filename));
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
