using Kaligraphy.DataClasses.Parsing;

namespace plugin_level5_preview.Preview.Subtitle
{
    class SubtitleCharacterDeserializer : TimeTravelersCharacterDeserializer<SubtitleCharacterDeserializerContext>
    {
        protected override bool TryDeserializeCharacter(SubtitleCharacterDeserializerContext context, int position, out int length,
            out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryDeserializeCharacter(context, position, out length, out textCharacter);

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
                    IsVisible = fontCharacter.IsVisible && context.IsSubtitle,
                    IsPersistent = fontCharacter.IsPersistent && context.IsSubtitle,
                    Character = fontCharacter.Character
                };
            }

            return true;
        }
    }
}
