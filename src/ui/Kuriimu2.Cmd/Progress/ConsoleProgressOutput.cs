using System;
using Kore.Progress;

namespace Kuriimu2.Cmd.Progress
{
    class ConsoleProgressOutput : BaseConcurrentProgressOutput
    {
        public ConsoleProgressOutput(int updateInterval) : base(updateInterval)
        {
        }

        protected override void OutputProgressInternal(double completion, string message)
        {
            var intCompletion = Convert.ToInt32(completion);
            var barFilled = new string('#', intCompletion / 2);
            var barEmpty = new string('-', 50 - intCompletion / 2);

            Console.Write($"\rProgress: {completion:0.00}% [{barFilled}{barEmpty}]");
        }
    }
}
