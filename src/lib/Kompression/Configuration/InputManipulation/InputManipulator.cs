using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Configuration.InputManipulation.Types;
using Kontract;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Configuration.InputManipulation
{
    class InputManipulator : IInputManipulator
    {
        private readonly IList<IInputManipulationType> _manipulations;

        public InputManipulator()
        {
            _manipulations = Array.Empty<IInputManipulationType>();
        }

        public InputManipulator(IList<IInputManipulationType> manipulations)
        {
            ContractAssertions.IsNotNull(manipulations, nameof(manipulations));

            _manipulations = manipulations;
        }

        public Stream Manipulate(Stream input)
        {
            foreach (var manipulation in _manipulations)
                input = manipulation.Manipulate(input);

            return input;
        }

        public void AdjustMatch(Match match)
        {
            foreach (var manipulation in _manipulations.Reverse())
                manipulation.AdjustMatch(match);
        }
    }
}
