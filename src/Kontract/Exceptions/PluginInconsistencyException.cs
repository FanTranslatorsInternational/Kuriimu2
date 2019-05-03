using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Exceptions
{
    public class PluginInconsistencyException : Exception
    {
        public PluginInconsistencyException(params string[] pluginNames) : base(CreateInconsistentList(pluginNames))
        {

        }

        private static string CreateInconsistentList(string[] pluginNames)
        {
            if (pluginNames == null || !pluginNames.Any())
                return "Some plugins create inconsistencies.";

            var result = $"The following plugins are inconsistent: {Environment.NewLine}{Environment.NewLine}";
            return result + string.Join(Environment.NewLine, pluginNames);
        }
    }
}
