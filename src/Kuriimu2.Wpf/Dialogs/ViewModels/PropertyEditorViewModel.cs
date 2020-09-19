using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Attributes;
using Kuriimu2.Wpf.Dialogs.Common;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    public sealed class PropertyEditorViewModel<TProperties> : Screen
    {
        private TProperties _object;
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

        public TProperties Object
        {
            get => _object;
            set
            {
                _object = value;

                Fields = GetFields(typeof(TProperties));
                NotifyOfPropertyChange(() => Fields);
            }
        }

        public Func<ValidationResult> ValidationCallback;

        public Dictionary<string, DynamicField> Fields { get; private set; }

        public void OKButton()
        {
            SetFields();

            if (ValidationCallback != null)
            {
                var results = ValidationCallback();

                if (results.CanClose)
                    TryCloseAsync(true);
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryCloseAsync(true);
            }
        }

        private Dictionary<string, DynamicField> GetFields(Type type, string name = "", object obj = null)
        {
            obj ??= _object;
            var result = new Dictionary<string, DynamicField>();

            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (prop.GetCustomAttribute<FormFieldIgnoreAttribute>() != null)
                    continue;

                var newName = string.IsNullOrEmpty(name) ? prop.Name : name + '.' + prop.Name;
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType == typeof(decimal))
                {
                    if (prop.SetMethod == null || prop.GetMethod == null)
                        continue;

                    var formFieldAttribute = prop.GetCustomAttribute<FormFieldAttribute>();

                    var label = (formFieldAttribute?.DisplayName ?? prop.Name) + " :";
                    var value = formFieldAttribute != null
                        ? Convert.ChangeType(prop.GetValue(obj), formFieldAttribute.Type)
                        : prop.GetValue(obj);
                    var maxLength = formFieldAttribute?.MaxLength ?? 0;
                    result.Add(newName, new DynamicField(label, value, maxLength, prop, obj));
                }
                else if (prop.PropertyType.IsClass || IsStruct(prop.PropertyType))
                {
                    var newObj = prop.GetValue(obj);
                    foreach (var field in GetFields(prop.PropertyType, newName, newObj))
                        result.Add(field.Key, field.Value);
                }
            }

            return result;
        }

        private void SetFields()
        {
            foreach (var field in Fields)
            {
                var formFieldAttribute = field.Value.PropertyInfo.GetCustomAttribute<FormFieldAttribute>();

                var newValue = formFieldAttribute != null
                    ? Convert.ChangeType(field.Value.Value, formFieldAttribute.Type)
                    : field.Value.Value;
                field.Value.PropertyInfo.SetValue(field.Value.Object, newValue);
            }
        }

        private bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsEnum;
        }
    }

    public sealed class DynamicField
    {
        public string Label { get; }
        public object Value { get; set; }
        public int MaxLength { get; }
        public PropertyInfo PropertyInfo { get; }
        public object Object { get; }

        public DynamicField(string label, object value, int maxLength, PropertyInfo info, object obj)
        {
            Label = label;
            Value = value;
            MaxLength = maxLength;
            PropertyInfo = info;
            Object = obj;
        }
    }
}
