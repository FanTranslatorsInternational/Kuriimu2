using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.Enums.Management.Dialog;
using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Progress;
using Kuriimu2.Cmd.Parsers;

namespace Kuriimu2.Cmd.Manager
{
    class ConsoleDialogManager(IArgumentGetter argumentGetter, IProgressContext progress) : IDialogManager
    {
        public IList<string> DialogOptions { get; } = [];

        public Task<bool> ShowDialog(DialogField[] fields)
        {
            progress.FinishProgress();

            foreach (DialogField field in fields)
                ProcessField(field);

            progress.StartProgress();

            return Task.FromResult(true);
        }

        private void ProcessField(DialogField field)
        {
            Console.Clear();

            string suffix = !string.IsNullOrEmpty(field.Text) ? $" for '{field.Text}'" : string.Empty;
            Console.WriteLine($"Input is requested{suffix}:");

            switch (field.Type)
            {
                case DialogFieldType.TextBox:
                    field.Result = argumentGetter.GetNextArgument();
                    break;

                case DialogFieldType.DropDown:
                    GetDropDownArgument(field);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported dialog field type {field.Type}.");
            }
        }

        private void GetDropDownArgument(DialogField field)
        {
            for (var i = 0; i < field.Options.Length; i++)
                Console.WriteLine($"[{i}] " + field.Options[i]);

            while (true)
            {
                string optionIndexArgument = argumentGetter.GetNextArgument();

                if (!int.TryParse(optionIndexArgument, out int optionIndex))
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
