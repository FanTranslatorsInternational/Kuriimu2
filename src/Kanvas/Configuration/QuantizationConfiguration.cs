using System;
using Kontract;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    public class QuantizationConfiguration : IQuantizationConfiguration
    {
        private int _taskCount = Environment.ProcessorCount;

        private Action<IQuantizationOptions> _configureAction;

        public IQuantizationConfiguration WithOptions(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));
            _configureAction = configure;

            return this;
        }

        public IQuantizationConfiguration WithTaskCount(int taskCount)
        {
            if (taskCount <= 0)
                throw new InvalidOperationException("Task count has to be greater than 0.");

            _taskCount = taskCount;

            return this;
        }

        public IQuantizer Build()
        {
            var options = new QuantizationOptions();
            _configureAction?.Invoke(options);

            options.WithTaskCount(_taskCount);

            return options;
        }
    }
}
