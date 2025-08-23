using System.Text;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_mt_framework_preview.Characters;

namespace plugin_mt_framework_preview.Texts
{
    class GmdCharacterParser : CharacterParser
    {
        protected override bool TryParseControlCode(CharacterParserContext context, int position, out int length, out ControlCodeCharacterData? controlCode)
        {
            controlCode = null;

            if (!TryReadCharacter(context, position, out length, out char character))
                return false;

            if (character is not '<')
                return false;

            position += length;

            var sb = new StringBuilder();
            var args = new List<string>();

            while (position < context.Data!.Length)
            {
                if (!TryReadCharacter(context, position, out int byteLength, out character))
                    return false;

                length += byteLength;
                position += byteLength;

                if (character is '<')
                    return false;

                if (character is '>')
                {
                    if (args.Count <= 0 && sb.Length <= 0)
                        return false;

                    if (sb.Length > 0)
                        args.Add(sb.ToString());

                    controlCode = new GmdControlCodeCharacterData
                    {
                        Code = args[0],
                        Arguments = args.Count <= 1 ? [] : args[1..],
                        IsVisible = false,
                        IsPersistent = false
                    };
                    return true;
                }

                if (character is ' ')
                {
                    if (sb.Length > 0)
                    {
                        args.Add(sb.ToString());
                        sb.Clear();
                    }

                    continue;
                }

                sb.Append(character);
            }

            return false;
        }

        protected override bool IsLineBreak(CharacterParserContext context, int position, out int length, out string lineBreak)
        {
            length = 0;
            lineBreak = string.Empty;

            if (!TryReadCharacter(context, position, out int byteCount, out char character))
                return false;

            length += byteCount;
            position += byteCount;

            if (position >= context.Data!.Length)
                return false;

            if (!TryReadCharacter(context, position, out byteCount, out char character1))
                return false;

            length += byteCount;

            bool isLineBreak = character == '\r' && character1 == '\n';

            if (!isLineBreak)
                return false;

            lineBreak = $"{character}{character1}";
            return true;
        }
    }
}
