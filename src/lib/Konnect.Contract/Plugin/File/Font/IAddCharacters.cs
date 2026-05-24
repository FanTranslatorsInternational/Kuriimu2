using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace Konnect.Contract.Plugin.File.Font;

/// <summary>
/// This interface allows the font adapter to add new characters through the UI.
/// </summary>
public interface IAddCharacters : IFontFilePluginState
{
    /// <summary>
    /// Creates a new character and allows the plugin to provide its derived type.
    /// </summary>
    /// <param name="codePoint">The code point this character represents.</param>
    /// <returns>CharacterInfo or a derived type.</returns>
    CharacterInfo CreateCharacterInfo(char codePoint);

    /// <summary>
    /// Adds a newly created character to the file and allows the plugin to perform any required adding steps.
    /// </summary>
    /// <param name="set">The font set to add the character to.</param>
    /// <param name="characterInfo">The characterInfo to add.</param>
    /// <returns>True if the character was added, False otherwise.</returns>
    bool AddCharacter(FontSet set, CharacterInfo characterInfo);
}