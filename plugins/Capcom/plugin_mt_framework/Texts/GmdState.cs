using System.Text;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Text;

namespace plugin_mt_framework.Texts
{
    class GmdState : ILoadFiles, ISaveFiles, IRenameEntries, IAddEntries, IRemoveEntries
    {
        private readonly Gmdv1 _gmd1 = new();
        private readonly Gmdv2 _gmd2 = new();

        private GmdVersion _version;
        private List<TextEntry> _texts;
        private bool _hasDeletedEntries;

        public IReadOnlyList<TextEntry> Texts => _texts;
        public IReadOnlyList<Guid>? PreviewGuids { get; } = [
            Guid.Parse("ef1074a3-78b9-4358-adeb-6b58c49173ea"),
            Guid.Parse("1280108e-010d-4bf0-a495-e614f340360c"),
            Guid.Parse("a1fecf11-70aa-49f1-af6f-498d5ff2de41")
        ];
        public ITextEntryPager? Pager { get; } = null;

        public bool CanSetNewEntryName { get; } = true;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            _hasDeletedEntries = false;

            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            if (!GmdSupport.TryGetVersion(fileStream, out GmdVersion version))
                throw new InvalidOperationException("GMD version unknown.");

            _version = version;
            _texts = version switch
            {
                GmdVersion.v1 => _gmd1.Load(fileStream),
                GmdVersion.v2 => _gmd2.Load(fileStream),
                _ => throw new InvalidOperationException("GMD version unknown.")
            };
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            switch (_version)
            {
                case GmdVersion.v1:
                    _gmd1.Save(_texts, fileStream);
                    break;

                case GmdVersion.v2:
                    _gmd2.Save(_texts, fileStream);
                    break;

                default:
                    throw new InvalidOperationException("GMD version unknown.");
            }
        }

        private bool IsContentChanged()
        {
            return _texts.Any(x => x.ContentChanged) || _hasDeletedEntries;
        }

        public bool RenameEntry(TextEntry entry, string name)
        {
            entry.Name = name;
            return true;
        }

        public bool RemoveEntry(TextEntry entry, TextEntryPage? page = null)
        {
            _texts.Remove(entry);

            _hasDeletedEntries = true;
            return true;
        }

        public TextEntry CreateEntry(TextEntryPage? page = null)
        {
            return new TextEntry
            {
                TextData = [],
                Encoding = Encoding.UTF8,
                ContentChanged = true
            };
        }

        public bool AddEntry(TextEntry entry, TextEntryPage? page = null)
        {
            _texts.Add(entry);
            return true;
        }
    }
}
