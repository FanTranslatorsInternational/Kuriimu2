﻿using System;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class HexForm : IKuriimuForm
    {
        public HexForm(FormInfo formInfo)
        {
            InitializeComponent();

            hexBox.LoadStream((formInfo.FileState.PluginState as IHexState)?.FileStream);
        }

        #region Forminterface methods

        public bool HasRunningOperations()
        {
            return false;
        }

        #endregion

        #region Update

        public void UpdateForm()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
