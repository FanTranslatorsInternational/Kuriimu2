using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    [Export(typeof(VC3GameAdapter))]
    [Export(typeof(IGameAdapter))]
    [Export(typeof(IGenerateGamePreviews))]
    [PluginInfo("84D2BD62-7AC6-459B-B3BB-3A65855135F6", "Valkyria Chronicles 3", "VC3GA", "IcySon55")]
    public sealed class VC3GameAdapter : IGameAdapter, IGenerateGamePreviews
    {
        private string BasePath => Path.Combine("plugins", ID);

        public string ID => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).ID;

        public string Name => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).Name;

        public string IconPath => Path.Combine("plugins", ID, "icon.png");

        public string Filename { get; set; }

        public IEnumerable<TextEntry> Entries { get; private set; }

        public void LoadEntries(IEnumerable<TextEntry> entries)
        {
            Entries = entries;
        }

        public IEnumerable<TextEntry> SaveEntries()
        {
            return Entries;
        }

        public Bitmap GeneratePreview(TextEntry entry)
        {
            var bgPath = Path.Combine(BasePath, "BG01.png");
            if (!File.Exists(bgPath)) return null;

            var img = (Bitmap)Image.FromFile(bgPath);

            var textTop = img.Height / 2 + img.Height / 2 / 2;

            var gfx = Graphics.FromImage(img);
            gfx.FillRectangle(new SolidBrush(Color.White), new Rectangle(16, textTop, 192, 48));

            gfx.DrawString(entry.EditedText, new Font("MS Sans Serif", 8), new SolidBrush(Color.Black), 20, textTop + 4);

            return img;
        }
    }
}
