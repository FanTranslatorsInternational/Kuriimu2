using Kaligraphy.DataClasses.Parsing;

namespace plugin_level5_preview.Preview.Narration
{
    internal class NarrationCharacterParser : TimeTravelersCharacterParser<TimeTravelersParserContext>
    {
        protected override bool TryParseCharacter(TimeTravelersParserContext context, int position, out int length, out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryParseCharacter(context, position, out length, out textCharacter);

            if (!isValid)
                return false;

            if (textCharacter is FontCharacterData fontCharacter)
            {
                textCharacter = new FontCharacterData
                {
                    IsVisible = fontCharacter.Character is not '＊' && fontCharacter.IsVisible, 
                    Character = fontCharacter.Character
                };
            }

            return isValid;
        }
    }
}
