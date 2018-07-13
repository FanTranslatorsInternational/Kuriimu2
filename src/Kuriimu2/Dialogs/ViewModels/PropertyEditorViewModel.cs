using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Attributes;
using Kuriimu2.Dialogs.Common;

namespace Kuriimu2.Dialogs.ViewModels
{
    public sealed class PropertyEditorViewModel<T> : Screen
    {
        private T _object;
        private DialogMode _mode = DialogMode.Edit;

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
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Images/menu-add.png"));
                else if (value == DialogMode.Edit)
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Images/menu-edit.png"));
            }
        }

        public T Object
        {
            get => _object;
            set
            {
                _object = value;

                Fields = new Dictionary<string, DynaField>();
                var props = _object.GetType().GetProperties().ToList();

                foreach (var prop in props)
                {
                    var ff = (FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute));
                    if ((FormFieldIgnoreAttribute)prop.GetCustomAttribute(typeof(FormFieldIgnoreAttribute)) != null) continue;

                    var df = new DynaField
                    {
                        Label = (ff?.DisplayName ?? prop.Name) + " :",
                        Value = ff != null ? Convert.ChangeType(prop.GetValue(_object), ff.Type) : prop.GetValue(_object),
                        MaxLength = ff?.MaxLength ?? 0
                    };
                    Fields.Add(prop.Name, df);
                }
                NotifyOfPropertyChange(() => Fields);
            }
        }

        public Func<ValidationResult> ValidationCallback;

        public Dictionary<string, DynaField> Fields { get; private set; }

        public void OKButton()
        {
            var props = _object.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                var ff = (FormFieldAttribute)prop.GetCustomAttribute(typeof(FormFieldAttribute));
                if ((FormFieldIgnoreAttribute)prop.GetCustomAttribute(typeof(FormFieldIgnoreAttribute)) != null) continue;

                var df = Fields[prop.Name];
                prop.SetValue(_object, ff != null ? Convert.ChangeType(df.Value, ff.Type) : df.Value);
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

    public sealed class DynaField
    {
        public string Label { get; set; }
        public object Value { get; set; }
        public int MaxLength { get; set; }
    }
}
