using System.IO;
using Kontract.Kompression.Models.PatternMatch;

namespace Kontract.Kompression.Interfaces.Configuration
{
    public interface IInputManipulator
    {
        Stream Manipulate(Stream input);

        void AdjustMatch(Match match);
    }
}
