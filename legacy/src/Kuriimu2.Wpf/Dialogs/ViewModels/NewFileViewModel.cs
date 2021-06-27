﻿using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kuriimu2.Wpf.Dialogs.Common;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    public sealed class NewFileViewModel : Screen
    {
        public string Title { get; set; } = "Edit Character";
        public BitmapImage Icon { get; private set; }


        public Func<ValidationResult> ValidationCallback;

        public Dictionary<string, DynamicField> Fields { get; private set; }

        public NewFileViewModel()
        {
            
        }

        public void OKButton()
        {
            
        }
    }
}
