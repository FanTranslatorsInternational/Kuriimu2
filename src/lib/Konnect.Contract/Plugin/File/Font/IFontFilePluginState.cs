using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace Konnect.Contract.Plugin.File.Font;

public interface IFontFilePluginState : IFilePluginState
{
    IReadOnlyList<FontSet> Sets { get; }

    #region Optional feature support checks

    bool CanAddCharacter => this is IAddCharacters;
    bool CanRemoveCharacter => this is IRemoveCharacters;

    #endregion

    #region Optional feature casting defaults

    CharacterInfo? AttemptCreateCharacterInfo(char codePoint) => (this as IAddCharacters)?.CreateCharacterInfo(codePoint);
    bool AttemptAddCharacter(FontSet set, CharacterInfo characterInfo) => (this as IAddCharacters)?.AddCharacter(set, characterInfo) ?? false;
    bool AttemptRemoveCharacter(FontSet set, CharacterInfo characterInfo) => (this as IRemoveCharacters)?.RemoveCharacter(set, characterInfo) ?? false;
    void AttemptRemoveAll(FontSet set) => (this as IRemoveCharacters)?.RemoveAll(set);

    #endregion
}