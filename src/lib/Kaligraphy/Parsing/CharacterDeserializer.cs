using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;

namespace Kaligraphy.Parsing
{
    public class CharacterDeserializer : ICharacterDeserializer
    {
        public IList<CharacterData> Deserialize(string text)
        {
            var result = new List<CharacterData>();

            var position = 0;
            while (position < text.Length)
            {
                CharacterData? character = DeserializeText(text, position, out int length);
                if (character is not null)
                    result.Add(character);

                position += length;
            }

            return result;
        }

        private CharacterData? DeserializeText(string text, int position, out int length)
        {
            if (TryDeserializeControlCode(text, position, out length, out ControlCodeCharacterData? controlCode))
                return controlCode;

            if (TryDeserializeCharacter(text, position, out length, out TextCharacterData? textCharacter))
                return textCharacter;

            return null;
        }

        protected virtual bool TryDeserializeControlCode(string text, int position, out int length,
            out ControlCodeCharacterData? controlCode)
        {
            length = 0;
            controlCode = null;

            return false;
        }

        protected virtual bool TryDeserializeCharacter(string text, int position, out int length,
            out TextCharacterData? textCharacter)
        {
            textCharacter = null;

            if (IsLineBreak(text, position, out length))
            {
                textCharacter = new LineBreakCharacterData();
                return true;
            }

            length = 1;

            textCharacter = new FontCharacterData { Character = text[position] };
            return true;
        }

        protected virtual bool IsLineBreak(string text, int position, out int length)
        {
            /* Check for \n or \r\n for valid line breaks */

            length = 1;

            if (text[position] == '\n')
                return true;

            if (position + 1 >= text.Length)
                return false;

            length = 2;
            return (text[position] == '\r' && text[position + 1] == '\n')
                   || (text[position] == '\\' && text[position + 1] == 'n');
        }
    }
}
