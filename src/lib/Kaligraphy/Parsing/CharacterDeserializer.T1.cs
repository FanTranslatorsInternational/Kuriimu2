using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;

namespace Kaligraphy.Parsing;

public class CharacterDeserializer<TContext> : ICharacterDeserializer
    where TContext : CharacterDeserializerContext, new()
{
    public IList<CharacterData> Deserialize(string text)
    {
        var result = new List<CharacterData>();
        var context = new TContext
        {
            Text = text
        };

        var position = 0;
        while (position < text.Length)
        {
            CharacterData? character = DeserializeText(context, position, out int length);
            if (character is not null)
                result.Add(character);

            position += length;
        }

        return result;
    }

    private CharacterData? DeserializeText(TContext context, int position, out int length)
    {
        if (TryDeserializeControlCode(context, position, out length, out ControlCodeCharacterData? controlCode))
            return controlCode;

        if (TryDeserializeCharacter(context, position, out length, out TextCharacterData? textCharacter))
            return textCharacter;

        return null;
    }

    protected virtual bool TryDeserializeControlCode(TContext context, int position, out int length,
        out ControlCodeCharacterData? controlCode)
    {
        length = 0;
        controlCode = null;

        return false;
    }

    protected virtual bool TryDeserializeCharacter(TContext context, int position, out int length,
        out TextCharacterData? textCharacter)
    {
        length = 0;
        textCharacter = null;

        if (context.Text is null)
            return false;

        if (IsLineBreak(context, position, out length, out string lineBreak))
        {
            textCharacter = new LineBreakCharacterData { IsVisible = false, LineBreak = lineBreak };
            return true;
        }

        length = 1;

        textCharacter = new FontCharacterData { Character = context.Text[position] };
        return true;
    }

    protected virtual bool IsLineBreak(TContext context, int position, out int length, out string lineBreak)
    {
        /* Check for \n or \r\n for valid line breaks */

        length = 1;
        lineBreak = string.Empty;

        if (context.Text is null)
            return false;

        if (context.Text[position] == '\n')
        {
            lineBreak = "\n";
            return true;
        }

        if (position + 1 >= context.Text.Length)
            return false;

        length = 2;
        bool isLineBreak = (context.Text[position] == '\r' && context.Text[position + 1] == '\n');

        if (!isLineBreak)
            return false;

        lineBreak = context.Text[position..(position + 2)];
        return true;
    }
}