using Kaligraphy.Contract.DataClasses.Parsing;
using plugin_level5_preview.Preview.CharacterData;

namespace plugin_level5_preview.Preview.Subtitle
{
    internal class SubtitleCharacterParser : TimeTravelersCharacterParser<SubtitleCharacterParserContext>
    {
        protected override bool TryParseCharacter(SubtitleCharacterParserContext context, int position, out int length, out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryParseCharacter(context, position, out length, out textCharacter);

            if (!isValid)
                return false;

            if (textCharacter is FuriganaCharacterData)
                return true;

            if (textCharacter is not FontCharacterData fontCharacter)
                return true;

            switch (fontCharacter.Character)
            {
                case '「':
                    textCharacter.IsVisible = false;
                    context.IsSubtitle = true;
                    break;

                case '」':
                    textCharacter.IsVisible = false;
                    context.IsSubtitle = false;
                    break;

                default:
                    textCharacter.IsVisible &= context.IsSubtitle;
                    textCharacter.IsPersistent &= context.IsSubtitle;
                    break;
            }

            return true;
        }
    }
}
