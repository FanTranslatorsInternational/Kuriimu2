using Kaligraphy.Contract.Parsing;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text
{
    public interface ITextFilePluginState : IFilePluginState
    {
        IReadOnlyList<TextEntry> Texts { get; }

        ICharacterParser? Parser { get; }
        ICharacterComposer? Composer { get; }
        ICharacterSerializer? Serializer { get; }
        ICharacterDeserializer? Deserializer { get; }

        #region Optional feature checks

        public bool CanAddEntry => this is IAddEntries;
        public bool CanDeleteEntry => this is IDeleteEntries;
        public bool CanRenameEntry => this is IRenameEntries;

        #endregion
    }
}
