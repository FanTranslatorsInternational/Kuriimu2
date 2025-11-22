using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace Konnect.Contract.Plugin.File.Font;

public interface IFontFilePluginState : IFilePluginState
{
    /// <summary>
    /// The list of characters provided by the state.
    /// </summary>
    IReadOnlyList<CharacterInfo> Characters { get; }

    #region Optional feature support checks

    bool CanAddCharacter => this is IAddCharacters;
    bool CanRemoveCharacter => this is IRemoveCharacters;

    #endregion

    #region Optional feature casting defaults

    CharacterInfo? AttemptCreateCharacterInfo(char codePoint) => (this as IAddCharacters)?.CreateCharacterInfo(codePoint);
    bool AttemptAddCharacter(CharacterInfo characterInfo) => (this as IAddCharacters)?.AddCharacter(characterInfo) ?? false;
    bool AttemptRemoveCharacter(CharacterInfo characterInfo) => (this as IRemoveCharacters)?.RemoveCharacter(characterInfo) ?? false;
    void AttemptRemoveAll() => (this as IRemoveCharacters)?.RemoveAll();

    #endregion
}