using System.Collections.Generic;
using Kompression.Configuration.InputManipulation.Types;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Configuration.InputManipulation
{
    class InputConfiguration : IInputConfiguration
    {
        private readonly IList<IInputManipulationType> _manipulations = new List<IInputManipulationType>();

        public IInputConfiguration Skip(int skip)
        {
            _manipulations.Add(new SkipInput(skip));

            return this;
        }

        public IInputConfiguration Reverse()
        {
            _manipulations.Add(new ReverseInput());
            return this;
        }

        public IInputConfiguration Prepend(int byteCount, byte value = 0)
        {
            _manipulations.Add(new PrependInput(byteCount, value));
            return this;
        }

        public IInputManipulator Build()
        {
            return new InputManipulator(_manipulations);
        }
    }
}
