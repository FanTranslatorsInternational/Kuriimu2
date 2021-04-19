using System;
using System.Collections.Generic;
using Kontract.Extensions;
using Kontract.Models.Font;

namespace Kontract.Interfaces.Plugins.State
{
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
        
        #region Optional features

        /// <summary>
        /// Creates a new character and allows the plugin to provide its derived type.
        /// </summary>
        /// <param name="codePoint">The code point this character represents.</param>
        /// <returns>CharacterInfo or a derived type.</returns>
        CharacterInfo CreateCharacterInfo(uint codePoint)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Adds a newly created character to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="characterInfo">The characterInfo to add.</param>
        /// <returns>True if the character was added, False otherwise.</returns>
        bool AddCharacter(CharacterInfo characterInfo)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Deletes an character and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="characterInfo">The character to be deleted.</param>
        /// <returns>True if the character was successfully deleted, False otherwise.</returns>
        bool RemoveCharacter(CharacterInfo characterInfo)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Removes all characters from the font state.
        /// </summary>
        void RemoveAll()
        {
            throw new InvalidOperationException();
        }
        
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an character.
        /// </summary>
        /// <param name="character">The character to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowCharacterProperties(CharacterInfo character)
        {
            throw new InvalidOperationException();
        }

        #endregion
        
        #region Optional feature support checks
        
        //TODO should check CreateCharacterInfo as well?
        public bool CanAddCharacter => this.ImplementsMethod(typeof(IFontState), nameof(AddCharacter));
        //TODO should check RemoveAll as well?
        public bool CanRemoveCharacter => this.ImplementsMethod(typeof(IFontState), nameof(RemoveCharacter));
        public bool CanShowCharacterProperties => this.ImplementsMethod(typeof(IFontState), nameof(ShowCharacterProperties));
        
        #endregion
    }
}
