using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;

namespace Kompression.Configuration
{
    internal class LempelZivOptionsBuilder(LempelZivOptions options) : ILempelZivOptionsBuilder
    {
        public ILempelZivOptionsBuilder WithDegreeOfParallelism(int taskCount)
        {
            options.TaskCount = taskCount;
            return this;
        }

        public ILempelZivOptionsBuilder ParseMatchesWith(CreateMatchParserDelegate parserDelegateDelegate)
        {
            options.CreateMatchParserDelegate = parserDelegateDelegate;
            return this;
        }
    }
}
