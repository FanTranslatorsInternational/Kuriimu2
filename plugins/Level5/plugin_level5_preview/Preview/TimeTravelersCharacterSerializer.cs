using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview
{
    class TimeTravelersCharacterSerializer : CharacterSerializer
    {
        protected override string? SerializeControlCode(ControlCodeCharacterData controlCode)
        {
            switch (controlCode)
            {
                case BlankControlCodeCharacterData blank:
                    return $"<BLANK{blank.Width}>";

                case IconControlCodeCharacterData icon:
                    return $"<ICON\"{icon.IconName}\">";

                case TipStartControlCodeCharacterData tip:
                    return $"<TIP{tip.TipNumber:000}>";

                case TipEndControlCodeCharacterData:
                    return "</TIP>";

                case GenericControlCodeCharacterData generic:
                    return $"<{generic.Code}>";

                default:
                    return null;
            }
        }
    }
}
