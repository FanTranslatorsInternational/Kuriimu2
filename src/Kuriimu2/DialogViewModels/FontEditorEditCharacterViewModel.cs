using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kuriimu2.DialogViewModels
{
    public sealed class FontEditorEditCharacterViewModel : Screen
    {
        private FontCharacter _character;
        private DialogMode _mode  = DialogMode.Edit;

        public string Title { get; set; } = "Edit Character";
        public BitmapImage Icon { get; private set; }
        public string Message { get; set; } = "Character Attributes:";
        public string Error { get; set; } = string.Empty;
        public int TextBoxWidth { get; set; } = 200;

        public DialogMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                if (value == DialogMode.Add)
                    Icon = new BitmapImage( new Uri("pack://application:,,,/Images/menu-add.png"));
                else if (value == DialogMode.Edit)
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Images/menu-edit.png"));
            }
        }

        public FontCharacter Character
        {
            get => _character;
            set
            {
                _character = value;

                Fields = new Dictionary<string, DynaField>();
                var props = _character.GetType().GetProperties().ToList();

                foreach (var prop in props)
                {
                    var ft = (FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute));
                    var df = new DynaField
                    {
                        Label = (ft?.DisplayName ?? prop.Name) + " :",
                        Value = ft != null ? Convert.ChangeType(prop.GetValue(_character), ft.Type) : prop.GetValue(_character),
                        MaxLength = ft?.MaxLength ?? 0
                    };

                    Fields.Add(prop.Name, df);
                }
                NotifyOfPropertyChange(() => Fields);
            }
        }

        public Func<ValidationResult> ValidationCallback;

        public Dictionary<string, DynaField> Fields { get; private set; }

        public FontEditorEditCharacterViewModel()
        {
            Mode = DialogMode.Edit;
        }

        public void OKButton()
        {
            var props = _character.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                var ft = (FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute));
                var df = Fields[prop.Name];

                prop.SetValue(_character, ft != null ? Convert.ChangeType(df.Value, ft.Type) : df.Value);
            }

            if (ValidationCallback != null)
            {
                var results = ValidationCallback();

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
    }

    public enum DialogMode
    {
        Add,
        Edit
    }

    public sealed class DynaField
    {
        public string Label { get; set; }
        public object Value { get; set; }
        public int MaxLength { get; set; }
    }

    public class ValidationResult
    {
        public bool CanClose { get; set; }
        public string ErrorMessage { get; set; }
    }
}
