using Kontract.Models.Font;

namespace Kontract.Interfaces.Plugins.State.Font
{
    /// <summary>
    /// Characters provide an extended properties dialog?
    /// </summary>
    public interface ICharactersHaveExtendedProperties
    {
        /// <summary>
        /// Opens the extended properties dialog for an character.
        /// </summary>
        /// <param name="character">The character to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowCharacterProperties(CharacterInfo character);
    }
}
