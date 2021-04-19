using Kontract.Models.Font;

namespace Kontract.Interfaces.Plugins.State.Font
{
    /// <summary>
    /// This interface allows the font adapter to delete characters through the UI.
    /// </summary>
    public interface IRemoveCharacters
    {
        /// <summary>
        /// Deletes an character and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="characterInfo">The character to be deleted.</param>
        /// <returns>True if the character was successfully deleted, False otherwise.</returns>
        bool RemoveCharacter(CharacterInfo characterInfo);

        /// <summary>
        /// Removes all characters from the font state.
        /// </summary>
        void RemoveAll();
    }
}
