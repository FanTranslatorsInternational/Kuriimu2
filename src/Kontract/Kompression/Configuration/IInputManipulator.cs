using System.IO;
using Kontract.Kompression.Model.PatternMatch;

namespace Kontract.Kompression.Configuration
{
    public interface IInputManipulator
    {
        Stream Manipulate(Stream input);

        void AdjustMatch(Match match);
    }
}
