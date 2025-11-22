using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Parsing;
using System.Text;

namespace Kaligraphy.Parsing;

public class CharacterParser<TContext> : ICharacterParser
    where TContext : CharacterParserContext, new()
{
    public IList<CharacterData> Parse(byte[] data, Encoding encoding)
    {
        var result = new List<CharacterData>();

        var context = new TContext
        {
            Data = data,
            EncodingDecoder = encoding.GetDecoder()
        };

        var position = 0;
        while (position < data.Length)
        {
            CharacterData? character = ParseCharacterData(context, position, out int length);
            if (character is not null)
                result.Add(character);

            position += length;
        }

        return result;
    }

    private CharacterData? ParseCharacterData(TContext context, int position, out int length)
    {
        if (TryParseControlCode(context, position, out length, out ControlCodeCharacterData? controlCode))
            return controlCode;

        if (TryParseCharacter(context, position, out length, out TextCharacterData? textCharacter))
            return textCharacter;

        return null;
    }

    protected virtual bool TryParseControlCode(TContext context, int position, out int length,
        out ControlCodeCharacterData? controlCode)
    {
        length = 0;
        controlCode = null;

        return false;
    }

    protected virtual bool TryParseCharacter(TContext context, int position, out int length,
        out TextCharacterData? textCharacter)
    {
        textCharacter = null;

        if (IsLineBreak(context, position, out length, out string lineBreak))
        {
            textCharacter = new LineBreakCharacterData { IsVisible = false, LineBreak = lineBreak };
            return true;
        }

        if (!TryReadCharacter(context, position, out length, out char character))
            return false;

        textCharacter = new FontCharacterData { Character = character };
        return true;
    }

    protected virtual bool IsLineBreak(TContext context, int position, out int length, out string lineBreak)
    {
        /* Check for \n or \r\n for valid line breaks */

        length = 0;
        lineBreak = string.Empty;

        if (!TryReadCharacter(context, position, out int byteCount, out char character))
            return false;

        length += byteCount;
        position += byteCount;

        if (character == '\n')
        {
            lineBreak = $"{character}";
            return true;
        }

        if (position >= context.Data!.Length)
            return false;

        if (!TryReadCharacter(context, position, out byteCount, out char character1))
            return false;

        length += byteCount;

        lineBreak = $"{character}{character1}";
        return character == '\r' && character1 == '\n';
    }

    protected static bool TryReadCharacter(TContext context, int position, out int length, out char character)
    {
        length = 0;
        character = '\0';

        if (context.Data is null || context.EncodingDecoder is null)
            return false;

        if (position >= context.Data.Length)
            return false;

        var buffer = new char[1];
        context.EncodingDecoder.Convert(context.Data[position..], buffer, false, out length, out var charLength, out _);

        if (charLength < 1)
            return false;

        character = buffer[0];
        return true;
    }

    protected static bool TryReadString(TContext context, int position, int textLength, out int length, out string text)
    {
        length = 0;
        text = string.Empty;

        if (context.Data is null || context.EncodingDecoder is null)
            return false;

        if (position >= context.Data.Length)
            return false;

        var buffer = new char[textLength];
        context.EncodingDecoder.Convert(context.Data[position..], buffer, false, out length, out var charLength, out _);

        if (charLength < textLength)
            return false;

        text = new string(buffer);
        return true;
    }
}