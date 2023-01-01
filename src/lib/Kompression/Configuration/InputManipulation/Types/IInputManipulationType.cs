using System.IO;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Configuration.InputManipulation.Types
{
    interface IInputManipulationType
    {
        Stream Manipulate(Stream input);

        void AdjustMatch(Match match);
    }
}
