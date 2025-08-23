using Kaligraphy.DataClasses.Parsing;

namespace plugin_level5_preview.Preview.Narration
{
    class NarrationCharacterDeserializer : TimeTravelersCharacterDeserializer<TimeTravelersDeserializerContext>
    {
        protected override bool TryDeserializeCharacter(TimeTravelersDeserializerContext context, int position, out int length,
            out TextCharacterData? textCharacter)
        {
            bool isValid = base.TryDeserializeCharacter(context, position, out length, out textCharacter);

            if (!isValid)
                return false;

            if (textCharacter is FontCharacterData fontCharacter)
            {
                textCharacter = new FontCharacterData
                {
                    IsVisible = fontCharacter.Character is not '＊' && fontCharacter.IsVisible,
                    IsPersistent = fontCharacter.Character is not '＊' && fontCharacter.IsPersistent,
                    Character = fontCharacter.Character
                };
            }

            return isValid;
        }
    }
}
