using System.Text;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview
{
    internal class TimeTravelersCharacterParser<TContext> : CharacterParser<TContext>
        where TContext : TimeTravelersParserContext, new()
    {
        protected override bool TryParseControlCode(TContext context, int position, out int length, out ControlCodeCharacterData? controlCode)
        {
            controlCode = null;

            if (!TryReadCharacter(context, position, out length, out char character))
                return false;

            if (character is '<')
            {
                if (IsTipStart(context, position, out length, out int tipNumber))
                {
                    controlCode = new TipStartControlCodeCharacterData { IsVisible = false, IsPersistent = false, TipNumber = tipNumber };
                    return true;
                }

                if (IsTipEnd(context, position, out length))
                {
                    controlCode = new TipEndControlCodeCharacterData { IsVisible = false, IsPersistent = false };
                    return true;
                }

                if (IsIcon(context, position, out length, out string iconName))
                {
                    controlCode = new IconControlCodeCharacterData { IsVisible = false, IsPersistent = false, IconName = iconName };
                    return true;
                }

                if (IsBlank(context, position, out length, out int width))
                {
                    controlCode = new BlankControlCodeCharacterData { IsVisible = false, IsPersistent = false, Width = width };
                    return true;
                }

                if (IsControlCode(context, position, out length, out string controlCodeText))
                {
                    controlCode = new GenericControlCodeCharacterData { IsVisible = false, IsPersistent = false, Code = controlCodeText };
                    return true;
                }
            }

            return false;
        }

        protected override bool TryParseCharacter(TContext context, int position, out int length, out TextCharacterData? textCharacter)
        {
            length = 0;
            textCharacter = null;

            if (IsLineBreak(context, position, out length, out string lineBreak))
            {
                textCharacter = new LineBreakCharacterData { IsVisible = false, LineBreak = lineBreak };
                return true;
            }

            if (!TryReadCharacter(context, position, out length, out char character))
                return false;

            if (character is '[')
            {
                context.IsFuriganaBottom = true;
                context.IsFuriganaTop = false;

                textCharacter = new FuriganaStartCharacterData { IsVisible = false, IsPersistent = false, Character = character };
                return true;
            }

            if (character is '/' && context.IsFuriganaBottom)
            {
                context.IsFuriganaBottom = false;
                context.IsFuriganaTop = true;

                textCharacter = new FuriganaSplitCharacterData { IsVisible = false, IsPersistent = false, Character = character };
                return true;
            }

            if (character is ']')
            {
                context.IsFuriganaBottom = false;
                context.IsFuriganaTop = false;

                textCharacter = new FuriganaEndCharacterData { IsVisible = false, IsPersistent = false, Character = character };
                return true;
            }

            textCharacter = new FontCharacterData { IsVisible = !context.IsFuriganaTop, IsPersistent = !context.IsFuriganaTop, Character = character };
            return true;
        }

        protected override bool IsLineBreak(TContext context, int position, out int length, out string lineBreak)
        {
            length = 0;
            lineBreak = string.Empty;

            if (!TryReadCharacter(context, position, out int byteCount, out char character))
                return false;

            length += byteCount;
            position += byteCount;

            if (character == '\n')
            {
                lineBreak = $"{character}";
                return true;
            }

            if (position >= context.Data!.Length)
                return false;

            if (!TryReadCharacter(context, position, out byteCount, out char character1))
                return false;

            length += byteCount;

            bool isLineBreak = (character == '\r' && character1 == '\n') ||
                               (character == '\\' && character1 == 'n');

            if (!isLineBreak)
                return false;

            lineBreak = $"{character}{character1}";
            return true;

        }

        private bool IsTipStart(TContext context, int position, out int length, out int tipNumber)
        {
            length = 0;
            tipNumber = 0;

            if (!TryReadString(context, position, 4, out length, out var text))
                return false;

            if (text is not "<TIP")
                return false;

            if (!TryReadString(context, position, 3, out var addLength, out var tipNumberText))
            {
                length += addLength;
                return false;
            }

            length += addLength;

            if (!TryReadCharacter(context, position, out addLength, out char character))
            {
                length += addLength;
                return false;
            }

            if (character is not '>')
                return false;

            length += addLength;

            return int.TryParse(tipNumberText, out tipNumber);
        }

        private bool IsTipEnd(TContext context, int position, out int length)
        {
            if (!TryReadString(context, position, 6, out length, out var text))
                return false;

            return text is "</TIP>";
        }

        private bool IsIcon(TContext context, int position, out int length, out string iconName)
        {
            length = 0;
            iconName = string.Empty;

            if (!TryReadString(context, position, 4, out length, out string text))
                return false;

            if (text is not "<ICON\"")
                return false;

            position += length;

            var sb = new StringBuilder();
            while (position < context.Data!.Length)
            {
                if (!TryReadCharacter(context, position, out int addLength, out char endChar1))
                    return false;

                length += addLength;
                position += addLength;

                if (endChar1 is not '\"')
                {
                    sb.Append(endChar1);
                    continue;
                }

                if (!TryReadCharacter(context, position, out addLength, out char endChar2))
                    return false;

                length += addLength;
                position += addLength;

                if (endChar2 is not '>')
                {
                    sb.Append(endChar1);
                    sb.Append(endChar2);
                    continue;
                }

                break;
            }

            iconName = sb.ToString();
            return true;
        }

        private bool IsBlank(TContext context, int position, out int length, out int width)
        {
            length = 0;
            width = -1;

            if (!TryReadString(context, position, 6, out length, out string text))
                return false;

            if (text is not "<BLANK")
                return false;

            position += length;

            var sb = new StringBuilder();
            while (position < context.Data!.Length)
            {
                if (!TryReadCharacter(context, position, out int addLength, out char endChar1))
                    return false;

                length += addLength;
                position += addLength;

                if (endChar1 is not '>')
                {
                    sb.Append(endChar1);
                    continue;
                }

                break;
            }

            return int.TryParse(sb.ToString(), out width);
        }

        private bool IsControlCode(TContext context, int position, out int length, out string controlCodeText)
        {
            length = 0;
            controlCodeText = string.Empty;

            if (!TryReadCharacter(context, position, out length, out char character))
                return false;

            if (character is not '<')
                return false;

            position += length;

            var sb = new StringBuilder();
            while (position < context.Data!.Length)
            {
                if (!TryReadCharacter(context, position, out int addLength, out char endChar1))
                    return false;

                length += addLength;
                position += addLength;

                if (endChar1 is not '>')
                {
                    sb.Append(endChar1);
                    continue;
                }

                break;
            }

            controlCodeText = sb.ToString();
            return true;
        }
    }
}
