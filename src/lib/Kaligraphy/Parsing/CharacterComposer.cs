using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;

namespace Kaligraphy.Parsing;

public class CharacterComposer : ICharacterComposer
{
    public byte[] Compose(IList<CharacterData> characters, Encoding encoding)
    {
        var result = new List<byte>();

        foreach (CharacterData character in characters)
        {
            byte[]? data = ComposeCharacterData(character, encoding);
            if (data is null)
                continue;

            result.AddRange(data);
        }

        return [.. result];
    }

    private byte[]? ComposeCharacterData(CharacterData character, Encoding encoding)
    {
        return character switch
        {
            ControlCodeCharacterData controlCode => ComposeControlCode(controlCode, encoding),
            TextCharacterData textCharacter => ComposeCharacter(textCharacter, encoding),
            _ => null
        };
    }

    protected virtual byte[]? ComposeControlCode(ControlCodeCharacterData controlCode, Encoding encoding)
    {
        return null;
    }

    protected virtual byte[]? ComposeCharacter(CharacterData character, Encoding encoding)
    {
        return character switch
        {
            LineBreakCharacterData lineBreak => encoding.GetBytes(lineBreak.LineBreak),
            FontCharacterData fontCharacter => encoding.GetBytes($"{(char)fontCharacter.Character}"),
            _ => null
        };
    }
}