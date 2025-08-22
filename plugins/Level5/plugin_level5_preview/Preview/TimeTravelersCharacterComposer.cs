using System.Text;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview
{
    class TimeTravelersCharacterComposer : CharacterComposer
    {
        protected override byte[]? ComposeControlCode(ControlCodeCharacterData controlCode, Encoding encoding)
        {
            switch (controlCode)
            {
                case BlankControlCodeCharacterData blank:
                    return encoding.GetBytes($"<BLANK{blank.Width}>");

                case IconControlCodeCharacterData icon:
                    return encoding.GetBytes($"<ICON\"{icon.IconName}\">");

                case TipStartControlCodeCharacterData tip:
                    return encoding.GetBytes($"<TIP{tip.TipNumber:000}>");

                case TipEndControlCodeCharacterData:
                    return encoding.GetBytes("</TIP>");

                case GenericControlCodeCharacterData generic:
                    return encoding.GetBytes($"<{generic.Code}>");

                default:
                    return null;
            }
        }
    }
}
