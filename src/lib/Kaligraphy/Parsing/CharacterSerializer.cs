using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;
using System.Text;

namespace Kaligraphy.Parsing
{
    public class CharacterSerializer : ICharacterSerializer
    {
        public string Serialize(IList<CharacterData> characters)
        {
            var result = new StringBuilder();

            foreach (CharacterData character in characters)
            {
                string? data = SerializeCharacterData(character);
                if (data is null)
                    continue;

                result.Append(data);
            }

            return result.ToString();
        }

        private string? SerializeCharacterData(CharacterData character)
        {
            switch (character)
            {
                case ControlCodeCharacterData controlCode:
                    return SerializeControlCode(controlCode);

                case TextCharacterData textCharacter:
                    return SerializeCharacter(textCharacter);
            }

            return null;
        }

        protected virtual string? SerializeControlCode(ControlCodeCharacterData controlCode)
        {
            return null;
        }

        protected virtual string? SerializeCharacter(CharacterData character)
        {
            switch (character)
            {
                case LineBreakCharacterData:
                    return "\n";

                case FontCharacterData fontCharacter:
                    return $"{fontCharacter.Character}";
            }

            return null;
        }
    }
}
