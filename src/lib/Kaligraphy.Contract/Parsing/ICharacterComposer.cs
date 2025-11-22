using System.Text;
using Kaligraphy.Contract.DataClasses.Parsing;

namespace Kaligraphy.Contract.Parsing;

public interface ICharacterComposer
{
    byte[] Compose(IList<CharacterData> characters, Encoding encoding);
}