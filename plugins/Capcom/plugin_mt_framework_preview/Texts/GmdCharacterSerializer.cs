using System.Text;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_mt_framework_preview.Characters;

namespace plugin_mt_framework_preview.Texts
{
    class GmdCharacterSerializer : CharacterSerializer
    {
        protected override string? SerializeControlCode(ControlCodeCharacterData controlCode)
        {
            switch (controlCode)
            {
                case GmdControlCodeCharacterData gmdControlCode:
                    var sb = new StringBuilder();
                    
                    sb.Append('<');
                    sb.Append(gmdControlCode.Code);

                    foreach(string argument in gmdControlCode.Arguments)
                    {
                        sb.Append(' ');
                        sb.Append(argument);
                    }

                    sb.Append('>');

                    return sb.ToString();
            }

            return null;
        }
    }
}
