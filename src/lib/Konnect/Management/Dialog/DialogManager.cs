using Konnect.Contract.DataClasses.Management.Dialog;
using Konnect.Contract.Management.Dialog;

namespace Konnect.Management.Dialog;

public class DialogManager : IDialogManager
{
    private readonly IDialogManager? _dialogManager;
    private readonly IList<string> _options;
    private int _optionIndex;

    /// <inheritdoc />
    public IList<string> DialogOptions { get; } = new List<string>();

    public DialogManager(IList<string> options)
    {
        _options = options;
    }

    public DialogManager(IDialogManager dialogManager, IList<string> options)
    {
        _dialogManager = dialogManager;
        _options = options;
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
            string option = _options[_optionIndex++];

            fields[fieldIndex++].Result = option;
            DialogOptions.Add(option);
        }

        // If all fields were already processed by predefined options
        if (fieldIndex >= fields.Length)
            return true;

        // Collect results from dialog manager if predefined options are exhausted
        DialogField[] subFields = fields.Skip(fieldIndex).ToArray();
        if (_dialogManager != null)
        {
            var result = await _dialogManager.ShowDialog(subFields);
            if (!result) return false;
        }

        foreach (DialogField subField in subFields)
        {
            if (subField.Result != null)
                DialogOptions.Add(subField.Result);
        }

        return true;
    }
}