using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_idea_factory.QUI
{
    /// <summary>
    /// This is the QUI TextAdapter for Kuriimu2.
    /// </summary>
    [Export(typeof(QuiAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("EEE98617-3F27-41EC-AD9A-1831419F8783", "IdeaFactory-QUI Text", "QUI", "IcySon55", "", "This is the QUI text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.qui")]
    public sealed class QuiAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private QUI _format;

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
                var sr = new StreamReader(input.FileData);

                while (!sr.EndOfStream)
                    if (Regex.IsMatch(sr.ReadLine() ?? string.Empty, @"^\(function \w+ \(\)$"))
                    {
                        sr.BaseStream.Position = 0;
                        return true;
                    }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(StreamInfo input)
        {
            _format = new QUI(input.FileData);
        }

        public void Save(StreamInfo output, int versionIndex = 0)
        {
            _format.Save(output.FileData);
        }

        public void Dispose() { }
    }
}
