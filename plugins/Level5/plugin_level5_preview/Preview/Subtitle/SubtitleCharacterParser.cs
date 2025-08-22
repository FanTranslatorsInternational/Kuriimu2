using Kaligraphy.DataClasses.Parsing;

namespace plugin_level5_preview.Preview.Subtitle
{
    internal class SubtitleCharacterParser : TimeTravelersCharacterParser<SubtitleCharacterParserContext>
    {
        protected override bool TryParseCharacter(SubtitleCharacterParserContext context, int position, out int length, out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryParseCharacter(context, position, out length, out textCharacter);

            if (!isValid)
                return false;

            if (textCharacter is not FontCharacterData fontCharacter)
                return true;

            if (fontCharacter.Character is '「')
            {
                textCharacter = new FontCharacterData
                {
                    IsVisible = false,
                    Character = fontCharacter.Character
                };

                context.IsSubtitle = true;
            }
            else if (fontCharacter.Character is '」')
            {
                textCharacter = new FontCharacterData
                {
                    IsVisible = false,
                    Character = fontCharacter.Character
                };

                context.IsSubtitle = false;
            }
            else
            {
                textCharacter = new FontCharacterData
                {
                    IsVisible = context.IsSubtitle,
                    Character = fontCharacter.Character
                };
            }

            return true;
        }
    }
}
