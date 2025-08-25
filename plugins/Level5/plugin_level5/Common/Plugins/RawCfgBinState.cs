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
    class RawCfgBinState : ILoadFiles, ISaveFiles, IAddEntries, IRemoveEntries
    {
        private readonly RawConfigurationReader _reader = new();
        private readonly RawConfigurationWriter _writer = new();
        private readonly EventTextParser _parser = new();
        private readonly EventTextComposer _composer = new();

        private EventTextConfiguration _config;
        private List<EventTextEntry> _texts;
        private bool _hasRemovedEntries;

        public IReadOnlyList<TextEntry> Texts => _texts;
        public IReadOnlyList<Guid>? PreviewGuids { get; } = [Guid.Parse("a21a4442-ead0-4707-9b3d-caf7806e3a47")];
        public ITextEntryPager? Pager { get; } = new EventPager();

        public bool CanSetNewEntryName => false;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            _hasRemovedEntries = false;

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

        public TextEntry CreateEntry(TextEntryPage? page = null)
        {
            uint hash = ((EventTextEntry)page?.Entries[0]!).Entry.Hash;
            int subId = ((EventTextEntry)page?.Entries[^1]!).Entry.SubId;

            var entry = new EventTextEntry
            {
                TextData = [],
                Encoding = _config.StringEncoding switch
                {
                    StringEncoding.Sjis => Encoding.GetEncoding("Shift-JIS"),
                    StringEncoding.Utf8 => Encoding.UTF8,
                    _ => throw new InvalidOperationException($"Unknown string encoding {_config.StringEncoding}.")
                },
                Entry = new EventText
                {
                    Hash = hash,
                    SubId = subId + 1
                },
                Name = $"0x{hash:X8}"
            };

            page?.Entries.Add(entry);

            return entry;
        }

        public bool AddEntry(TextEntry entry, TextEntryPage? page = null)
        {
            var eventEntry = (EventTextEntry)entry;

            _texts.Add(eventEntry);
            _config.Texts = [.. _config.Texts.Append(eventEntry.Entry)];

            page?.Entries.Add(eventEntry);

            return true;
        }

        public bool RemoveEntry(TextEntry entry, TextEntryPage? page = null)
        {
            var eventEntry = (EventTextEntry)entry;

            _texts.Remove(eventEntry);
            _config.Texts = [.. _config.Texts.Where(t => t != eventEntry.Entry)];

            page?.Entries.Remove(entry);

            _hasRemovedEntries = true;
            return true;
        }

        private bool IsContentChanged()
        {
            return Texts.Any(x => x.ContentChanged) || _hasRemovedEntries;
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
