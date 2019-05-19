using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract;
using Kontract.Interfaces.Intermediate;
using Kontract.Models;
using Kuriimu2_WinForms.MainForms.Models;

namespace Kuriimu2_WinForms.MainForms
{
    public partial class Batch : Form
    {
        private readonly PluginLoader _pluginLoader;
        private ComboBoxElement _selectedBatchType => cmbBatchType.SelectedItem as ComboBoxElement;

        public Batch(PluginLoader pluginLoader)
        {
            InitializeComponent();

            _pluginLoader = pluginLoader;

            LoadBatchTypes();
            UpdateBatchVariants();
            UpdateBatchMethods();

            cmbBatchType.SelectedIndexChanged += CmbBatchType_SelectedIndexChanged;
            cmbBatchMethod.SelectedIndexChanged += CmbBatchMethod_SelectedIndexChanged;
        }

        private void LoadBatchTypes()
        {
            if (_pluginLoader.GetAdapters<ICipherAdapter>().Any())
                cmbBatchType.Items.Add(new ComboBoxElement(typeof(ICipherAdapter), "Cipher"));
            //if (_pluginLoader.GetAdapters<ICompressionAdapter>().Any())
            //cmbBatchType.Items.Add(new ComboBoxElement(typeof(ICompressionAdapter), "Compression"));
            //if (_pluginLoader.GetAdapters<IHashAdapter>().Any())
            //cmbBatchType.Items.Add(new ComboBoxElement(typeof(IHashAdapter), "Hash"));
        }

        private void UpdateBatchVariants()
        {
            cmbBatchVariant.Items.Clear();

            if (_selectedBatchType.Value is ICipherAdapter)
            {
                foreach (var cipher in _pluginLoader.GetAdapters<ICipherAdapter>())
                    cmbBatchVariant.Items.Add(new ComboBoxElement(cipher, cipher.Name));
            }
            //else if (_selectedBatchType.Value is ICompressionAdapter)
            //{

            //}
            //else if(_selectedBatchType.Value is IHashAdapter)
            //{

            //}
        }

        private void UpdateBatchMethods()
        {
            cmbBatchMethod.Items.Clear();

            if (_selectedBatchType.Value is ICipherAdapter cipherAdapter)
            {
                cmbBatchMethod.Items.Add(new ComboBoxElement(
                    new Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>>(cipherAdapter.Decrypt), "Decrypt"));
                cmbBatchMethod.Items.Add(new ComboBoxElement(
                    new Func<Stream, Stream, IProgress<ProgressReport>, Task<bool>>(cipherAdapter.Encrypt), "Encrypt"));
            }
            //else if (_selectedBatchType.Value is ICompressionAdapter)
            //{

            //}
            //else if(_selectedBatchType.Value is IHashAdapter)
            //{

            //}
        }

        private void BtnBrowseInput_Click(object sender, EventArgs e)
        {

        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {

        }

        private void BtnBatchProcess_Click(object sender, EventArgs e)
        {

        }

        private void CmbBatchMethod_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CmbBatchType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
