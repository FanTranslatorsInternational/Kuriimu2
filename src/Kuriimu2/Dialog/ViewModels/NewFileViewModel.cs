using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kuriimu2.Dialog.Common;

namespace Kuriimu2.Dialog.ViewModels
{
    public sealed class NewFileViewModel : Screen
    {
        private FontCharacter _character;

        public string Title { get; set; } = "Edit Character";
        public BitmapImage Icon { get; private set; }


        public Func<ValidationResult> ValidationCallback;

        public Dictionary<string, DynaField> Fields { get; private set; }

        public NewFileViewModel()
        {
            
        }

        public void OKButton()
        {
            
        }
    }
}
