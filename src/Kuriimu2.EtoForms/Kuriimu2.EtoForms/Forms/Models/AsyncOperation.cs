using System;
using System.Collections.Generic;
using System.Text;

namespace Kuriimu2.EtoForms.Forms.Models
{
    class AsyncOperation
    {
        public event EventHandler Toggled;

        public bool IsRunning { get; private set; } = true;
    }
}
