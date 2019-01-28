using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;

namespace plugin_idea_factory.QUI
{
    /// <summary>
    /// This is the QUI TextAdapter for Kuriimu2.
    /// </summary>
    [Export(typeof(QuiAdapter))]
    [Export(typeof(ITextAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("EEE98617-3F27-41EC-AD9A-1831419F8783", "IdeaFactory-QUI Text", "QUI", "IcySon55", "", "This is the QUI text adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.qui")]
    public sealed class QuiAdapter : ITextAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private QUI _format;

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
                using (var sr = new StreamReader(File.OpenRead(filename)))
                {
                    while (!sr.EndOfStream)
                    {
                        if (Regex.IsMatch(sr.ReadLine() ?? string.Empty, @"^\(function \w+ \(\)$"))
                            return true;
                    }
                    return false;
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
                _format = new QUI(File.OpenRead(filename));
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _format.Save(File.Create(filename));
        }

        public void Dispose() { }
    }
}
