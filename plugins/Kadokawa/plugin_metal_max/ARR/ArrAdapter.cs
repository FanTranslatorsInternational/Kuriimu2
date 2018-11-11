using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace plugin_metal_max.ARR
{
    [Export(typeof(ITextAdapter))]
    [Export(typeof(ITextAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("B6C58C25-4E1C-4B9C-ABCF-DE905B1BBF51", "MM3-ARR Text", "ARR", "IcySon55, BuddyRoach", "", "This is the Metal Max 3 ARR adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.arr")]
    public sealed class ArrAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private ARR _format;

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
                    var count = br.ReadInt32();

                    var pointers = new List<short>();

                    for (var i = 0; i < count; i++)
                    {
                        for (var j = 0; j < 7; j++)
                        {
                            var p = br.ReadInt16();
                            if (p == 0) continue;
                            pointers.Add(p);
                        }
                    }

                    return pointers.Last() < br.BaseStream.Length && pointers.Last() >= br.BaseStream.Length - 64;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
                _format = new ARR(File.OpenRead(filename));
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
