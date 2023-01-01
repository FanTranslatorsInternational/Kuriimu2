using System.Collections.Generic;
using Kontract.Interfaces.Plugins.State.Font;
using Kontract.Models.Plugins.State.Font;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be a font format and exposes properties to retrieve and modify character and glyph data from the state.
    /// </summary>
    public interface IFontState : IPluginState
    {
        /// <summary>
        /// The list of characters provided by the state.
        /// </summary>
        IReadOnlyList<CharacterInfo> Characters { get; }

        /// <summary>
        /// Character baseline.
        /// </summary>
        float Baseline { get; set; }

        /// <summary>
        /// Character descent line.
        /// </summary>
        float DescentLine { get; set; }

        #region Optional feature support checks

        /// <summary>
        /// If the plugin can add new characters.
        /// </summary>
        public bool CanAddCharacter => this is IAddCharacters;

        /// <summary>
        /// If the plugin can remove characters.
        /// </summary>
        public bool CanRemoveCharacter => this is IRemoveCharacters;

        #endregion

        #region Optional feature casting defaults

        /// <summary>
        /// Casts and executes <see cref="IAddCharacters.CreateCharacterInfo"/>.
        /// </summary>
        /// <param name="codePoint">The code point this character represents.</param>
        /// <returns><see cref="CharacterInfo"/> or a derived type.</returns>
        CharacterInfo AttemptCreateCharacterInfo(uint codePoint) => ((IAddCharacters)this).CreateCharacterInfo(codePoint);

        /// <summary>
        /// Casts and executes <see cref="IAddCharacters.AddCharacter"/>.
        /// </summary>
        /// <param name="characterInfo">The <see cref="CharacterInfo"/> to add.</param>
        /// <returns><c>true</c>, if the character was added, <c>false</c> otherwise.</returns>
        bool AttemptAddCharacter(CharacterInfo characterInfo) => ((IAddCharacters)this).AddCharacter(characterInfo);

        /// <summary>
        /// Casts and executes <see cref="IRemoveCharacters.RemoveCharacter"/>.
        /// </summary>
        /// <param name="characterInfo">The <see cref="CharacterInfo"/> to be deleted.</param>
        /// <returns><c>true</c>, if the character was successfully deleted, <c>false</c> otherwise.</returns>
        bool AttemptCreateCharacterInfo(CharacterInfo characterInfo) => ((IRemoveCharacters)this).RemoveCharacter(characterInfo);

        /// <summary>
        /// Casts and executes <see cref="IRemoveCharacters.RemoveAll"/>.
        /// </summary>
        void AttemptRemoveAll() => ((IRemoveCharacters)this).RemoveAll();

        #endregion
    }
}
