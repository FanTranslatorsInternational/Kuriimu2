using System.Text;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Text;
using plugin_level5.Common.ConfigBinary;
using plugin_level5.Common.ConfigBinary.Models;

namespace plugin_level5.Common.Plugins
{
    class RawCfgBinState : ITextFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly RawConfigurationReader _reader = new();
        private readonly RawConfigurationWriter _writer = new();
        private readonly EventTextParser _parser = new();
        private readonly EventTextComposer _composer = new();

        private EventTextConfiguration _config;
        private List<EventTextEntry> _texts;

        public IReadOnlyList<TextEntry> Texts => _texts;
        public IReadOnlyList<Guid>? Previews { get; } = null;
        public ITextEntryPager? Pager { get; } = new EventPager();

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            Configuration<RawConfigurationEntry> config = _reader.Read(fileStream, StringEncoding.Sjis);
            _config = _parser.Parse(config);

            PopulateTextEntries();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            UpdateTextEntries();

            Configuration<RawConfigurationEntry> config = _composer.Compose(_config);
            _writer.Write(config, fileStream);
        }

        private bool IsContentChanged()
        {
            return Texts.Any(x => x.ContentChanged);
        }

        private void PopulateTextEntries()
        {
            Encoding encoding = _config.StringEncoding switch
            {
                StringEncoding.Sjis => Encoding.GetEncoding("Shift-JIS"),
                StringEncoding.Utf8 => Encoding.UTF8,
                _ => throw new InvalidOperationException($"Unknown string encoding {_config.StringEncoding}.")
            };

            _texts = [];

            foreach (EventText entry in _config.Texts)
            {
                _texts.Add(new EventTextEntry
                {
                    Name = $"0x{entry.Hash:X8}",
                    TextData = encoding.GetBytes(entry.Text ?? string.Empty),
                    Encoding = encoding,
                    Entry = entry
                });
            }
        }

        private void UpdateTextEntries()
        {
            foreach (EventTextEntry entry in _texts.Where(x => x.ContentChanged))
            {
                entry.Entry.Text = entry.Encoding.GetString(entry.TextData);
                entry.ContentChanged = false;
            }
        }
    }

    class EventTextEntry : TextEntry
    {
        public required EventText Entry { get; set; }
    }

    class EventPager : ITextEntryPager
    {
        public TextEntryPage[] Page(IReadOnlyList<TextEntry> entries)
        {
            var pages = new List<TextEntryPage>();

            foreach (var group in entries.Cast<EventTextEntry>().GroupBy(e => e.Entry.Hash))
            {
                pages.Add(new TextEntryPage
                {
                    Name = $"Page {pages.Count + 1}",
                    Entries = [.. group]
                });
            }

            return [.. pages];
        }
    }
}
