namespace Kontract.Interfaces.Plugins.State.Font
{
    /// <summary>
    /// Characters provide an extended properties dialog?
    /// </summary>
    public interface ICharactersHaveExtendedProperties
    {
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an character.
        /// </summary>
        /// <param name="character">The character to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowCharacterProperties(FontCharacter character);
    }
}
