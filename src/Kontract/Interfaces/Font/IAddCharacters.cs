namespace Kontract.Interfaces.Font
{
    /// <summary>
    /// This interface allows the font adapter to add new characters through the UI.
    /// </summary>
    public interface IAddCharacters
    {
        /// <summary>
        /// Adds a newly created character to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="character"></param>
        /// <returns>True if the character was added, False otherwise.</returns>
        bool AddCharacter(FontCharacter2 character);
    }
}
