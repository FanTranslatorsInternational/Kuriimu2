using System;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kontract.Models.Dialog;
using Kuriimu2.Cmd.Parsers;

namespace Kuriimu2.Cmd.Manager
{
    class ConsoleDialogManager : IDialogManager
    {
        private readonly IArgumentGetter _argumentGetter;
        private readonly IProgressContext _progress;

        public ConsoleDialogManager(IArgumentGetter argumentGetter, IProgressContext progress)
        {
            ContractAssertions.IsNotNull(argumentGetter, nameof(argumentGetter));
            ContractAssertions.IsNotNull(progress, nameof(progress));

            _argumentGetter = argumentGetter;
            _progress = progress;
        }

        public void ShowDialog(DialogField[] fields)
        {
            _progress.FinishProgress();

            foreach (var field in fields)
                ProcessField(field);

            _progress.StartProgress();
        }

        private void ProcessField(DialogField field)
        {
            Console.Clear();

            var suffix = !string.IsNullOrEmpty(field.Text) ? $" for '{field.Text}'" : string.Empty;
            Console.WriteLine($"Input is requested{suffix}:");

            switch (field.Type)
            {
                case DialogFieldType.TextBox:
                    field.Result = _argumentGetter.GetNextArgument();
                    break;

                case DialogFieldType.DropDown:
                    GetDropDownArgument(field);
                    break;
            }
        }

        private void GetDropDownArgument(DialogField field)
        {
            for (var i = 0; i < field.Options.Length; i++)
                Console.WriteLine($"[{i}] " + field.Options[i]);

            while (true)
            {
                var optionIndexArgument = _argumentGetter.GetNextArgument();

                if (!int.TryParse(optionIndexArgument, out var optionIndex))
                {
                    Console.WriteLine($"'{optionIndexArgument}' is not a valid number.");
                    continue;
                }

                if (optionIndex >= field.Options.Length)
                {
                    Console.WriteLine($"Index '{optionIndexArgument}' was out of bounds.");
                    continue;
                }

                field.Result = field.Options[optionIndex];
                break;
            }
        }
    }
}
