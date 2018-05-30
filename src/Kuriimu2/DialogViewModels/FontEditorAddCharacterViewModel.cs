using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Caliburn.Micro;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kuriimu2.DialogViewModels
{
    public sealed class FontEditorAddCharacterViewModel : Screen
    {
        private FontCharacter _character;
        private Func<ValidationResult> _validateClose;

        public FontEditorAddCharacterViewModel(FontCharacter character, Func<ValidationResult> validationCallback)
        {
            _character = character;
            _validateClose = validationCallback;
            Fields = new Dictionary<string, DynaField>();

            var props = _character.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                var ft = ((FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute)));
                var df = new DynaField { Label = (ft?.DisplayName ?? prop.Name) + " :" };

                df.Value = ft != null ? Convert.ChangeType(prop.GetValue(_character), ft.Type) : prop.GetValue(_character);
                df.MaxLength = ft?.MaxLength ?? 0;

                Fields.Add(prop.Name, df);
            }
        }

        public void OKButton()
        {
            var props = _character.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                var ft = ((FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute)));
                var df = Fields[prop.Name];

                prop.SetValue(_character, ft != null ? Convert.ChangeType(df.Value, ft.Type) : df.Value);
            }

            if (_validateClose != null)
            {
                var results = _validateClose();

                if (results.CanClose)
                    TryClose(true);
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryClose(true);
            }
        }

        public string Label { get; set; } = "Character Attributes:";

        public string Error { get; set; } = string.Empty;

        public Dictionary<string, DynaField> Fields { get; }
    }

    public sealed class DynaField
    {
        public string Label { get; set; }
        public object Value { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
    }

    public class ValidationResult
    {
        public bool CanClose { get; set; }
        public string ErrorMessage { get; set; }
    }
}
