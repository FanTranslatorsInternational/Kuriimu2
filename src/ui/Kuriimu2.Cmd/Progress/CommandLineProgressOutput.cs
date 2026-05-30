using Konnect.Progress;
using System;

namespace Kuriimu2.Cmd.Progress
{
    internal class CommandLineProgressOutput(string preText, int updateInterval) : ConcurrentProgressOutput(updateInterval)
    {
        protected override void OutputProgressInternal(double completion, string message)
        {
            Console.Write($"{preText} - {completion:0.00}%\r");
        }
    }
}
