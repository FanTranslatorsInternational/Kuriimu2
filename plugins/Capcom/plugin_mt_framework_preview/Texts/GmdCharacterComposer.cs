using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_mt_framework_preview.Characters;

namespace plugin_mt_framework_preview.Texts
{
    class GmdCharacterComposer : CharacterComposer
    {
        protected override byte[]? ComposeControlCode(ControlCodeCharacterData controlCode, Encoding encoding)
        {
            switch (controlCode)
            {
                case GmdControlCodeCharacterData gmdControlCode:
                    var result = new List<byte>();

                    result.AddRange(encoding.GetBytes(['<']));

                    result.AddRange(encoding.GetBytes(gmdControlCode.Code));

                    foreach (string arg in gmdControlCode.Arguments)
                    {
                        result.AddRange(encoding.GetBytes([' ']));
                        result.AddRange(encoding.GetBytes(arg));
                    }

                    result.AddRange(encoding.GetBytes(['>']));

                    return [.. result];
            }

            return null;
        }

        protected override byte[]? ComposeCharacter(CharacterData character, Encoding encoding)
        {
            switch (character)
            {
                case LineBreakCharacterData lineBreak:
                    return encoding.GetBytes(lineBreak.LineBreak);
            }

            return base.ComposeCharacter(character, encoding);
        }
    }
}
