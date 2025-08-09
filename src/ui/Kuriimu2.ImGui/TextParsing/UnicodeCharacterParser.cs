using System;
using System.Globalization;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;

namespace Kuriimu2.ImGui.TextParsing
{
    class UnicodeCharacterParser : CharacterParser
    {
        protected override CharacterData? ParseCharacter(ParseContext context, int position, out int length)
        {
            if (IsUnicode(context, position, out int unicodeLength, out length))
            {
                Span<byte> data = context.Data.AsSpan(position + (length - unicodeLength), unicodeLength);
                return new FontCharacterData { Character = ushort.Parse(context.Encoding.GetString(data), NumberStyles.HexNumber) };
            }

            return base.ParseCharacter(context, position, out length);
        }

        private static bool IsUnicode(ParseContext context, int position, out int unicodeLength, out int length)
        {
            length = 0;
            unicodeLength = 0;

            if (!TryReadCharacter(context, position, out int byteCount, out char character))
                return false;

            if (character != '\\')
                return false;

            length += byteCount;
            position += byteCount;

            if (position >= context.Data.Length)
                return false;

            if (!TryReadCharacter(context, position, out byteCount, out character))
                return false;

            if (character != 'u')
                return false;

            length += byteCount;
            position += byteCount;

            for (var i = 0; i < 4; i++)
            {
                if (position >= context.Data.Length)
                    return false;

                if (!TryReadCharacter(context, position, out byteCount, out character))
                    return false;

                if (character is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                    return false;

                length += byteCount;
                position += byteCount;
                unicodeLength += byteCount;
            }

            return true;
        }
    }
}
