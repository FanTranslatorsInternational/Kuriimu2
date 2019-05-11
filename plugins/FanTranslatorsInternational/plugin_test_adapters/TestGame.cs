using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Text;

namespace plugin_test_adapters
{
    [Export(typeof(IPlugin))]
    [PluginInfo("Test-Game-Id", "WinForms TestPreview")]
    public class TestGame : IGameAdapter, IGenerateGamePreviews
    {
        public string ID => throw new NotImplementedException();

        public string Name => "WinForms Test Preview";

        public string IconPath => "";

        public string Filename { get; set; }

        public IEnumerable<TextEntry> Entries => throw new NotImplementedException();

        public Bitmap GeneratePreview(TextEntry entry)
        {
            var image = new Bitmap(100, 100);
            var g = Graphics.FromImage(image);

            g.FillRectangle(Brushes.AliceBlue, new Rectangle(40, 40, 20, 20));
            g.DrawString(entry.EditedText, new Font("Times New Roman", 1), Brushes.OrangeRed, 30, 30);

            return image;
        }

        public void LoadEntries(IEnumerable<TextEntry> entries)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TextEntry> SaveEntries()
        {
            throw new NotImplementedException();
        }
    }
}
