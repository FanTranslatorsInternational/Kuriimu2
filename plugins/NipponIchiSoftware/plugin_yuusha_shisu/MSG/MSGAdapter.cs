using System.Collections.Generic;
using System.Composition;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_yuusha_shisu.MSG
{
    [Export(typeof(MsgAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("plugin_yuusha_shisu_msg", "Death of a Hero", "MSG", "StorMyu")]
    [PluginExtensionInfo("*.bin")]
    public sealed class MsgAdapter : ITextAdapter, ILoadFiles, ISaveFiles
    {
        private MSG _format;

        #region Properties

        public IEnumerable<TextEntry> Entries => _format?.Entries;

        public string NameFilter => @".*";

        public int NameMaxLength => 0;

        public string LineEndings { get; set; } = "\n";

        public bool LeaveOpen { get; set; }

        #endregion

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode node)
        {
            _format = new MSG(input.FileData);
        }

        public void Save(StreamInfo output, PhysicalDirectoryNode node, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }


    }
}
