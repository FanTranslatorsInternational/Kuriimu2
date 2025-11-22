using Kaligraphy.Contract.DataClasses.Parsing;

namespace Kaligraphy.Contract.Parsing;

public interface ICharacterSerializer
{
    string Serialize(IList<CharacterData> characters, bool includeControlCodes);
}