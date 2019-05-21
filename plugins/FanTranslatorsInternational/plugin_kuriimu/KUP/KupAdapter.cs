using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_kuriimu.KUP
{
    [Export(typeof(KupAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("963E7A09-61C4-4A80-94BA-427134F1A5B8", "Kuriimu Text Archive", "KUP", "IcySon55", "", "This is the KUP text adapter for Kuriimu.")]
    [PluginExtensionInfo("*.kup")]
    public sealed class KupAdapter : ITextAdapter, IIdentifyFiles, ICreateFiles, ILoadFiles, ISaveFiles, IAddEntries
    {
        private KUP _format;

        #region Properties

        public IEnumerable<TextEntry> Entries
        {
            get => _format?.Entries;
            set => _format.Entries = value.ToList();
        }

        public string NameFilter => @".*";

        public int NameMaxLength => 0;

        public string LineEndings { get; set; } = "\n";

        public bool LeaveOpen { get; set; } = false;

        #endregion

        public bool Identify(StreamInfo input, BaseReadOnlyDirectoryNode fileSystem)
        {
            var result = true;

            try
            {
                using (var sr = new StreamReader(input.FileData, Encoding.UTF8, true, 0x1000, LeaveOpen))
                {
                    sr.ReadLine(); // Skip the XML declaration
                    if (!sr.ReadLine().StartsWith("<kup"))
                        result = false;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Create()
        {
            _format = new KUP();
        }

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fileSystem)
        {
            _format = KUP.Load(input.FileData);
        }

        public void Save(StreamInfo output, PhysicalDirectoryNode fileSystem, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public TextEntry NewEntry()
        {
            return new TextEntry();
        }

        public bool AddEntry(TextEntry entry)
        {
            _format.Entries.Add(entry);
            return true;
        }

        public void Dispose() { }
    }
}
