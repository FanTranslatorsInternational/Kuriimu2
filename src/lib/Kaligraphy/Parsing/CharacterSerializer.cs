using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;
using System.Text;

namespace Kaligraphy.Parsing;

public class CharacterSerializer : ICharacterSerializer
{
    public string Serialize(IList<CharacterData> characters, bool includeControlCodes)
    {
        var result = new StringBuilder();

        foreach (CharacterData character in characters)
        {
            string? data = SerializeCharacterData(character, includeControlCodes);
            if (data is null)
                continue;

            result.Append(data);
        }

        return result.ToString();
    }

    private string? SerializeCharacterData(CharacterData character, bool includeControlCodes)
    {
        return character switch
        {
            ControlCodeCharacterData controlCode when includeControlCodes => SerializeControlCode(controlCode),
            TextCharacterData textCharacter => SerializeCharacter(textCharacter),
            _ => null
        };
    }

    protected virtual string? SerializeControlCode(ControlCodeCharacterData controlCode)
    {
        return null;
    }

    protected virtual string? SerializeCharacter(CharacterData character)
    {
        return character switch
        {
            LineBreakCharacterData lineBreak => lineBreak.LineBreak,
            FontCharacterData fontCharacter => $"{(char)fontCharacter.Character}",
            _ => null
        };
    }
}