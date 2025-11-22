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
        switch (character)
        {
            case ControlCodeCharacterData controlCode:
                return ComposeControlCode(controlCode, encoding);

            case TextCharacterData textCharacter:
                return ComposeCharacter(textCharacter, encoding);
        }

        return null;
    }

    protected virtual byte[]? ComposeControlCode(ControlCodeCharacterData controlCode, Encoding encoding)
    {
        return null;
    }

    protected virtual byte[]? ComposeCharacter(CharacterData character, Encoding encoding)
    {
        switch (character)
        {
            case LineBreakCharacterData lineBreak:
                return encoding.GetBytes(lineBreak.LineBreak);

            case FontCharacterData fontCharacter:
                return encoding.GetBytes($"{(char)fontCharacter.Character}");
        }

        return null;
    }
}