namespace Kontract.Interfaces.Font
{
    /// <summary>
    /// This interface allows the font adapter to delete characters through the UI.
    /// </summary>
    public interface IDeleteCharacters
    {
        /// <summary>
        /// Deletes an character and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="character">The character to be deleted.</param>
        /// <returns>True if the character was successfully deleted, False otherwise.</returns>
        bool DeleteCharacter(FontCharacter character);
    }
}
