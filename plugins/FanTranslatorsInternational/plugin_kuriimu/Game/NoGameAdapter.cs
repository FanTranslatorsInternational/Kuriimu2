using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using Kontract.Attributes;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Text;

namespace plugin_kuriimu.Game
{
    [Export(typeof(NoGameAdapter))]
    [Export(typeof(IGameAdapter))]
    [PluginInfo("68CC696C-E169-456C-AFAC-4DC61C577CD6", "No Game", "NGA", "IcySon55")]
    public sealed class NoGameAdapter : IGameAdapter
    {
        public string ID => ((PluginInfoAttribute)typeof(NoGameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).ID;

        public string Name => ((PluginInfoAttribute)typeof(NoGameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).Name;

        public string IconPath => Path.Combine("Images", "no-game.png");

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
    }
}