using Kaligraphy.Contract.DataClasses.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview.Narration
{
    internal class NarrationCharacterParser : TimeTravelersCharacterParser<TimeTravelersParserContext>
    {
        protected override bool TryParseCharacter(TimeTravelersParserContext context, int position, out int length, out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryParseCharacter(context, position, out length, out textCharacter);

            if (!isValid)
                return false;

            if (textCharacter is FuriganaCharacterData)
                return true;

            if (textCharacter is not FontCharacterData fontCharacter)
                return true;

            textCharacter.IsVisible &= fontCharacter.Character is not '＊';
            textCharacter.IsPersistent &= fontCharacter.Character is not '＊';

            return true;
        }
    }
}
