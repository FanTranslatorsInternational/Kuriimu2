using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_metal_max.NAM
{
    [Export(typeof(NamAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("18F83F20-3FB7-46DC-9028-EE00D658F029", "Metal Max 3: NAM Text", "NAM", "IcySon55", "", "This is the Metal Max 3 NAM text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.nam")]
    public sealed class NamAdapter : ITextAdapter, ILoadFiles, ISaveFiles
    {
        private NAM _format;

        #region Properties

        public IEnumerable<TextEntry> Entries => _format?.Entries;

        public string NameFilter => @".*";

        public int NameMaxLength => 0;

        public string LineEndings { get; set; } = "\n";

        public bool LeaveOpen { get; set; }

        #endregion

        public void Load(StreamInfo input)
        {
            var arrFile = Path.ChangeExtension(input.FileName, ".ARR");

            if (File.Exists(input.FileName) && File.Exists(arrFile))
                _format = new NAM(input.FileData, File.OpenRead(arrFile), input.FileName);
            else if (File.Exists(input.FileName))
                _format = new NAM(input.FileData, null, input.FileName);
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            var arrFile = Path.ChangeExtension(output.FileName, ".ARR");

            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
