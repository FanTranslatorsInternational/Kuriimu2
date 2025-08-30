using Konnect.Progress;
using System;

namespace Kuriimu2.Cmd.Progress
{
    class ConsoleProgressOutput(int updateInterval) : ConcurrentProgressOutput(updateInterval)
    {
        protected override void OutputProgressInternal(double completion, string message)
        {
            var intCompletion = Convert.ToInt32(completion);
            var barFilled = new string('#', intCompletion / 2);
            var barEmpty = new string('-', 50 - intCompletion / 2);

            Console.Write($"\rProgress: {completion:0.00}% [{barFilled}{barEmpty}]");
        }
    }
}
