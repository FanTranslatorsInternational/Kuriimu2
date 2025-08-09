using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;

namespace Kaligraphy.Contract.Parsing
{
    public interface ICharacterParser
    {
        IList<CharacterData> Parse(string text);

        IList<CharacterData> Parse(byte[] data, Encoding encoding);
    }
}
