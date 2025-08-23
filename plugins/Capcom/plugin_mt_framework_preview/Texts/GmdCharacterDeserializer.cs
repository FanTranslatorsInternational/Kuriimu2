using System.Text;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_mt_framework_preview.Characters;

namespace plugin_mt_framework_preview.Texts
{
    class GmdCharacterDeserializer : CharacterDeserializer
    {
        protected override bool TryDeserializeControlCode(CharacterDeserializerContext context, int position, out int length,
            out ControlCodeCharacterData? controlCode)
        {
            length = 0;
            controlCode = null;

            if (context.Text is null)
                return false;

            if (context.Text[position] != '<')
                return false;

            int endIndex = context.Text.IndexOf('>', position);
            if (endIndex < 0)
                return false;

            if (context.Text.IndexOf('<', position + 1, endIndex - position - 1) >= 0)
                return false;

            var sb = new StringBuilder();
            var args = new List<string>();

            for (int i = position + 1; i < endIndex; i++)
            {
                if (context.Text[i] is ' ')
                {
                    if (sb.Length > 0)
                    {
                        args.Add(sb.ToString());
                        sb.Clear();
                    }

                    continue;
                }

                sb.Append(context.Text[i]);
            }

            if (sb.Length > 0)
                args.Add(sb.ToString());

            if (args.Count <= 0)
                return false;

            length = endIndex - position + 1;
            controlCode = new GmdControlCodeCharacterData
            {
                Code = args[0],
                Arguments = args.Count <= 1 ? [] : args[1..],
                IsVisible = false,
                IsPersistent = false
            };

            return true;
        }

        protected override bool IsLineBreak(CharacterDeserializerContext context, int position, out int length, out string lineBreak)
        {
            bool isValid = base.IsLineBreak(context, position, out length, out lineBreak);
            
            if (!isValid)
                return false;

            lineBreak = "\r\n";
            return true;
        }
    }
}
