using System;
using System.Collections.Generic;
using Kontract;
using Kontract.Kanvas.Interfaces.Configuration;
using Kontract.Kanvas.Interfaces.Quantization;

namespace Kanvas.Configuration
{
    public class QuantizationConfiguration : IQuantizationConfiguration
    {
        private int _taskCount = Environment.ProcessorCount;

        private List<Action<IQuantizationOptions>> _configureActions = new List<Action<IQuantizationOptions>>();

        public IQuantizationConfiguration ConfigureOptions(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));

            _configureActions.Add(configure);

            return this;
        }

        public IQuantizationConfiguration WithTaskCount(int taskCount)
        {
            if (taskCount <= 0)
                throw new InvalidOperationException("Task count has to be greater than 0.");

            _taskCount = taskCount;

            return this;
        }

        public IQuantizationConfiguration Clone()
        {
            var configuration=new QuantizationConfiguration();

            foreach (var configureAction in _configureActions)
                configuration.ConfigureOptions(configureAction);

            configuration.WithTaskCount(_taskCount);

            return configuration;
        }

        public IQuantizer Build()
        {
            var options = new QuantizationOptions();
            foreach (var configureAction in _configureActions)
                configureAction(options);

            options.WithTaskCount(_taskCount);

            return options;
        }
    }
}
