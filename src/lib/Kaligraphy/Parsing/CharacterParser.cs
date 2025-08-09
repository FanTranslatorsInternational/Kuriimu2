using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;

namespace Kaligraphy.Parsing
{
    public class CharacterParser : ICharacterParser
    {
        public IList<CharacterData> Parse(byte[] data, Encoding encoding)
        {
            var result = new List<CharacterData>();

            var context = new ParseContext
            {
                Data = data,
                Encoding = encoding,
                MinByteCount = encoding.GetByteCount("\0"),
                MaxByteCount = encoding.GetMaxByteCount(1)
            };

            var position = 0;
            while (position < data.Length)
            {
                CharacterData? character = ParseCharacterData(context, position, out int length);
                if (character is not null)
                    result.Add(character);

                position += length;
            }

            return result;
        }

        private CharacterData? ParseCharacterData(ParseContext context, int position, out int length)
        {
            if (TryParseControlCode(context, position, out length, out ControlCodeCharacterData? controlCode))
                return controlCode;

            if (TryParseCharacter(context, position, out length, out TextCharacterData? textCharacter))
                return textCharacter;

            return null;
        }

        protected virtual bool TryParseControlCode(ParseContext context, int position, out int length,
            out ControlCodeCharacterData? controlCode)
        {
            length = 0;
            controlCode = null;

            return false;
        }

        protected virtual bool TryParseCharacter(ParseContext context, int position, out int length,
            out TextCharacterData? textCharacter)
        {
            textCharacter = null;

            if (IsLineBreak(context, position, out length))
            {
                textCharacter = new LineBreakCharacterData();
                return true;
            }

            if (!TryReadCharacter(context, position, out length, out char character))
                return false;

            textCharacter = new FontCharacterData { Character = character };
            return true;
        }

        protected virtual bool IsLineBreak(ParseContext context, int position, out int length)
        {
            /* Check for \n or \r\n for valid line breaks */

            length = 0;

            if (!TryReadCharacter(context, position, out int byteCount, out char character))
                return false;

            length += byteCount;
            position += byteCount;

            if (character == '\n')
                return true;

            if (position >= context.Data.Length)
                return false;

            if (!TryReadCharacter(context, position, out byteCount, out char character1))
                return false;

            length += byteCount;

            return character == '\r' && character1 == '\n';
        }

        protected static bool TryReadCharacter(ParseContext context, int position, out int length, out char character)
        {
            length = 0;
            character = '\0';

            var buffer = new char[1];
            for (int i = context.MinByteCount; i < context.MaxByteCount; i++)
            {
                if (position + i > context.Data.Length)
                    return false;

                if (!context.Encoding.TryGetChars(context.Data.AsSpan(position, i), buffer, out _))
                    continue;

                length = i;
                character = buffer[0];

                return true;
            }

            return false;
        }
    }
}
