using System.Collections.Generic;
using System.Globalization;
using Kuriimu2.ImGui.TextParsing.Models;

namespace Kuriimu2.ImGui.TextParsing
{
    class CharacterParser
    {
        public virtual IList<CharacterData> Parse(string text)
        {
            var result = new List<CharacterData>();

            var position = 0;
            while (position < text.Length)
            {
                CharacterData? character = GetCharacter(text, position, out int length);
                if (character != null)
                    result.Add(character);

                position += length;
            }

            return result;
        }

        protected virtual CharacterData GetCharacter(string text, int position, out int length)
        {
            if (IsLineBreak(text, position, out length))
                return new LineBreakCharacterData();
            
            if (IsUnicode(text, position, out length))
            {
                return new FontCharacterData
                {
                    Character = ushort.Parse(text[(position + 2)..(position + 6)], NumberStyles.HexNumber)
                };
            }

            length = 1;
            return new FontCharacterData
            {
                Character = text[position]
            };
        }

        private bool IsLineBreak(string text, int position, out int length)
        {
            length = 1;

            if (text[position] == '\n')
                return true;

            if (position + 1 >= text.Length)
                return false;

            length = 2;
            return (text[position] == '\r' && text[position + 1] == '\n')
                || (text[position] == '\\' && text[position + 1] == 'n');
        }

        private bool IsUnicode(string text, int position, out int length)
        {
            length = 1;

            if (text[position] != '\\')
                return false;

            if (position + 1 >= text.Length)
                return false;

            length = 2;
            if (text[position + 1] != 'u')
                return false;

            if (position + 5 >= text.Length)
                return false;

            length = 6;
            if (text[position + 2] is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
            if (text[position + 3] is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
            if (text[position + 4] is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
            if (text[position + 5] is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;

            return true;
        }
    }
}
