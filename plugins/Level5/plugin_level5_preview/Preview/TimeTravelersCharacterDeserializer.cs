using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview
{
    class TimeTravelersCharacterDeserializer<TContext> : CharacterDeserializer<TContext>
        where TContext : TimeTravelersDeserializerContext, new()
    {
        protected override bool TryDeserializeControlCode(TContext context, int position, out int length,
            out ControlCodeCharacterData? controlCode)
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

            controlCode = null;
            return false;
        }

        protected override bool TryDeserializeCharacter(TContext context, int position, out int length,
            out TextCharacterData? textCharacter)
        {
            length = 0;
            textCharacter = null;

            if (context.Text is null)
                return false;

            if (IsLineBreak(context, position, out length, out string lineBreak))
            {
                textCharacter = new LineBreakCharacterData { IsVisible = false, LineBreak = lineBreak };
                return true;
            }

            length = 1;

            if (context.Text[position] is '[')
            {
                context.IsFuriganaBottom = true;
                context.IsFuriganaTop = false;

                textCharacter = new FuriganaStartCharacterData { IsVisible = false, IsPersistent = false, Character = context.Text[position] };
                return true;
            }

            if (context.Text[position] is '/' && context.IsFuriganaBottom)
            {
                context.IsFuriganaBottom = false;
                context.IsFuriganaTop = true;

                textCharacter = new FuriganaSplitCharacterData { IsVisible = false, IsPersistent = false, Character = context.Text[position] };
                return true;
            }

            if (context.Text[position] is ']')
            {
                context.IsFuriganaBottom = false;
                context.IsFuriganaTop = false;

                textCharacter = new FuriganaEndCharacterData { IsVisible = false, IsPersistent = false, Character = context.Text[position] };
                return true;
            }

            textCharacter = new FontCharacterData { IsVisible = !context.IsFuriganaTop, IsPersistent = !context.IsFuriganaTop, Character = context.Text[position] };
            return true;
        }

        protected override bool IsLineBreak(TContext context, int position, out int length, out string lineBreak)
        {
            length = 1;
            lineBreak = string.Empty;

            if (context.Text is null)
                return false;

            if (context.Text[position] == '\n')
            {
                lineBreak = "\n";
                return true;
            }

            if (position + 1 >= context.Text.Length)
                return false;

            length = 2;
            bool isLineBreak = (context.Text[position] == '\r' && context.Text[position + 1] == '\n')
                               || (context.Text[position] == '\\' && context.Text[position + 1] == 'n');

            if (!isLineBreak)
                return false;

            lineBreak = context.Text[position..(position + 2)];
            return true;
        }

        private bool IsTipStart(TimeTravelersDeserializerContext context, int position, out int length, out int tipNumber)
        {
            length = 8;
            tipNumber = 0;

            if (context.Text is null)
                return false;

            if (position + length >= context.Text.Length)
                return false;

            if (context.Text[position..(position + 4)] != "<TIP")
                return false;

            if (context.Text[position + length - 1] != '>')
                return false;

            return int.TryParse(context.Text[(position + 4)..(position + 7)], out tipNumber);
        }

        private bool IsTipEnd(TimeTravelersDeserializerContext context, int position, out int length)
        {
            length = 6;

            if (context.Text is null)
                return false;

            if (position + length >= context.Text.Length)
                return false;

            return context.Text[position..(position + length)] == "</TIP>";
        }

        private bool IsIcon(TimeTravelersDeserializerContext context, int position, out int length, out string iconName)
        {
            length = 6;
            iconName = string.Empty;

            if (context.Text is null)
                return false;

            if (position + length >= context.Text.Length)
                return false;

            if (context.Text[position..(position + length)] != "<ICON\"")
                return false;

            int closeTagPosition = context.Text.IndexOf("\">", position + length, StringComparison.InvariantCulture);
            if (closeTagPosition < 0)
                return false;

            length = closeTagPosition - position + 2;
            iconName = context.Text[(position + 6)..closeTagPosition];

            return true;
        }

        private bool IsBlank(TimeTravelersDeserializerContext context, int position, out int length, out int width)
        {
            length = 6;
            width = -1;

            if (context.Text is null)
                return false;

            if (position + length >= context.Text.Length)
                return false;

            if (context.Text[position..(position + length)] != "<BLANK")
                return false;

            int closeTagPosition = context.Text.IndexOf('>', position + length);
            if (closeTagPosition < 0)
                return false;

            length = closeTagPosition - position + 1;
            return int.TryParse(context.Text[(position + 6)..closeTagPosition], out width);
        }

        private bool IsControlCode(TimeTravelersDeserializerContext context, int position, out int length, out string controlCodeText)
        {
            length = 2;
            controlCodeText = string.Empty;

            if (context.Text is null)
                return false;

            if (context.Text[position] != '<')
                return false;

            int endIndex = context.Text.IndexOf('>', position);
            if (endIndex < 0)
                return false;

            if (context.Text.IndexOf('<', position + 1, endIndex - position - 1) >= 0)
                return false;

            length = endIndex - position + 1;
            controlCodeText = context.Text[(position + 1)..endIndex];

            return true;
        }
    }
}
