using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers.Dialogs;
using Kontract.Models.Managers.Dialogs;

namespace Kore.Implementation.Managers.Dialogs
{
    /// <summary>
    /// Implementation of predefined options to an <see cref="IDialogManager"/>.
    /// </summary>
    class PredefinedDialogManager : IDialogManager
    {
        private readonly IDialogManager _dialogManager;
        private readonly IList<string> _options;
        private int _optionIndex;

        public IList<string> DialogOptions { get; }

        public PredefinedDialogManager(IDialogManager dialogManager, IList<string> options)
        {
            if (dialogManager == null && (options == null || options.Count <= 0))
                throw new InvalidOperationException("A dialog manager or predefined options have to be given.");

            DialogOptions = new List<string>();

            _dialogManager = dialogManager;
            _options = options ?? Array.Empty<string>();
        }

        /// <inheritdoc />
        public async Task<bool> ShowDialog(params DialogField[] fields)
        {
            // If no dialog Manager is given and not enough predefined options are available.
            if (_dialogManager == null && _options.Count - _optionIndex < fields.Length)
                throw new InvalidOperationException("Not enough predefined dialog options.");

            // Collect predefined options for each field
            var fieldIndex = 0;
            while (_optionIndex < _options.Count && fieldIndex < fields.Length)
            {
                var option = _options[_optionIndex++];

                fields[fieldIndex++].Result = option;
                DialogOptions.Add(option);
            }

            // If all fields were already processed by predefined options
            if (fieldIndex >= fields.Length)
                return true;

            // Collect results from dialog manager if predefined options are exhausted
            var subFields = fields.Skip(fieldIndex).ToArray();
            if (_dialogManager != null)
            {
                var result = await _dialogManager.ShowDialog(subFields);
                if (!result) return false;
            }

            // Add results from the dialog Manager to the results
            foreach (var subField in subFields)
                DialogOptions.Add(subField.Result);

            return true;
        }
    }
}
