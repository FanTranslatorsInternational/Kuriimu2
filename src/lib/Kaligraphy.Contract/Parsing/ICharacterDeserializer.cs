using Kaligraphy.Contract.DataClasses.Parsing;

namespace Kaligraphy.Contract.Parsing;

public interface ICharacterDeserializer
{
    IList<CharacterData> Deserialize(string text);
}