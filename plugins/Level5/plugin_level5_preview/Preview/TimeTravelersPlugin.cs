using System.Text;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;
using plugin_level5_preview.Preview.Narration;
using plugin_level5_preview.Preview.Subtitle;

namespace plugin_level5_preview.Preview
{
    public class TimeTravelersPlugin : IGamePlugin
    {
        private IGamePluginState? _narrationState;
        private IGamePluginState? _subtitleState;

        public Guid PluginId => Guid.Parse("a21a4442-ead0-4707-9b3d-caf7806e3a47");
        public PluginMetadata Metadata => new()
        {
            Author = ["onepiecefreak"],
            Name = "Time Travelers",
            Publisher = "Level5",
            Developer = "Level5",
            Platform = ["3DS", "Vita", "Psp"],
            LongDescription = "Preview plugin for Time Travelers."
        };

        public IGamePluginState CreatePluginState(UPath filePath, IReadOnlyList<TextEntry> entries, IPluginFileManager pluginFileManager)
        {
            if (entries.Count <= 0)
                return _narrationState ??= new TimeTravelersNarrationState(pluginFileManager);

            var isNarration = false;
            if (TryReadCharacter(entries[0].TextData, entries[0].Encoding.GetDecoder(), 0, out _, out char character))
                isNarration = character is '＊';

            if (isNarration)
                return _narrationState ??= new TimeTravelersNarrationState(pluginFileManager);

            return _subtitleState ??= new TimeTravelersSubtitleState(pluginFileManager);
        }

        private static bool TryReadCharacter(byte[] data, Decoder encoding, int position,
            out int length, out char character)
        {
            length = 0;
            character = '\0';

            if (position >= data.Length)
                return false;

            var chars = new char[1];
            encoding.Convert(data[position..], chars, false, out length, out int _, out bool _);

            character = chars[0];
            return true;
        }
    }
}
