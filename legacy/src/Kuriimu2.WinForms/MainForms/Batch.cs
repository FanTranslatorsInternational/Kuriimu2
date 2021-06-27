﻿namespace Kuriimu2.WinForms.MainForms
{
    //public partial class Batch : Form
    //{
    //    private readonly PluginLoader _pluginLoader;
    //    private ComboBoxElement _selectedBatchType => cmbBatchType.SelectedItem as ComboBoxElement;
    //    private ComboBoxElement _selectedBatchVariant => cmbBatchVariant.SelectedItem as ComboBoxElement;
    //    private ComboBoxElement _selectedBatchMethod => cmbBatchMethod.SelectedItem as ComboBoxElement;

    //    public Batch(PluginLoader pluginLoader)
    //    {
    //        InitializeComponent();

    //        txtBatchInputDirectory.Text = Settings.Default.BatchInputDirectory;
    //        txtBatchOutputDirectory.Text = Settings.Default.BatchOutputDirectory;

    //        _pluginLoader = pluginLoader;

    //        UpdateBatchTypes();
    //        UpdateBatchVariants();
    //        UpdateBatchMethods();

    //        cmbBatchType.SelectedIndexChanged += CmbBatchType_SelectedIndexChanged;
    //        cmbBatchMethod.SelectedIndexChanged += CmbBatchMethod_SelectedIndexChanged;
    //        cmbBatchVariant.SelectedIndexChanged += CmbBatchVariant_SelectedIndexChanged;

    //        UpdateForm();
    //    }

    //    private void UpdateForm()
    //    {
    //        if ((Type)_selectedBatchType.Value == typeof(IHashAdapter))
    //        {
    //            pnlHash.Visible = true;
    //            Size = new Size(Size.Width, 430);
    //            txtBatchOutputDirectory.Enabled = false;
    //            btnBrowseOutput.Enabled = false;
    //        }
    //        else
    //        {
    //            pnlHash.Visible = false;
    //            Size = new Size(Size.Width, 215);
    //            txtBatchOutputDirectory.Enabled = true;
    //            btnBrowseOutput.Enabled = true;
    //        }
    //    }

    //    private void UpdateBatchTypes()
    //    {
    //        cmbBatchType.Items.Clear();

    //        if (_pluginLoader.GetAdapters<ICipherAdapter>().Any())
    //            cmbBatchType.Items.Add(new ComboBoxElement(typeof(ICipherAdapter), "Cipher"));
    //        if (_pluginLoader.GetAdapters<ICompressionAdapter>().Any())
    //            cmbBatchType.Items.Add(new ComboBoxElement(typeof(ICompressionAdapter), "Compression"));
    //        if (_pluginLoader.GetAdapters<IHashAdapter>().Any())
    //            cmbBatchType.Items.Add(new ComboBoxElement(typeof(IHashAdapter), "Hash"));

    //        var count = cmbBatchType.Items.Count;
    //        cmbBatchType.Enabled = count > 0;
    //        if (count > 0)
    //            cmbBatchType.SelectedIndex = 0;
    //    }

    //    private void UpdateBatchVariants()
    //    {
    //        cmbBatchVariant.SelectedIndexChanged -= CmbBatchVariant_SelectedIndexChanged;
    //        cmbBatchVariant.Items.Clear();

    //        // TODO: Use plugin loader methods that take in a type
    //        if ((Type)_selectedBatchType.Value == typeof(ICipherAdapter))
    //        {
    //            foreach (var cipher in _pluginLoader.GetAdapters<ICipherAdapter>())
    //                cmbBatchVariant.Items.Add(new ComboBoxElement(cipher, cipher.Name));
    //        }
    //        else if ((Type)_selectedBatchType.Value == typeof(ICompressionAdapter))
    //        {
    //            foreach (var cipher in _pluginLoader.GetAdapters<ICompressionAdapter>())
    //                cmbBatchVariant.Items.Add(new ComboBoxElement(cipher, cipher.Name));
    //        }
    //        else if ((Type)_selectedBatchType.Value == typeof(IHashAdapter))
    //        {
    //            foreach (var cipher in _pluginLoader.GetAdapters<IHashAdapter>())
    //                cmbBatchVariant.Items.Add(new ComboBoxElement(cipher, cipher.Name));
    //        }

    //        var count = cmbBatchVariant.Items.Count;
    //        cmbBatchVariant.Enabled = count > 0;
    //        if (count > 0)
    //            cmbBatchVariant.SelectedIndex = 0;

    //        cmbBatchVariant.SelectedIndexChanged += CmbBatchVariant_SelectedIndexChanged;
    //    }

    //    private void UpdateBatchMethods()
    //    {
    //        cmbBatchMethod.SelectedIndexChanged -= CmbBatchMethod_SelectedIndexChanged;
    //        cmbBatchMethod.Items.Clear();

    //        if ((Type)_selectedBatchType.Value == typeof(ICipherAdapter))
    //        {
    //            SetCipherMethods();
    //        }
    //        else if ((Type)_selectedBatchType.Value == typeof(ICompressionAdapter))
    //        {
    //            SetCompressionMethods();
    //        }
    //        else if ((Type)_selectedBatchType.Value == typeof(IHashAdapter))
    //        {
    //            SetHashMethods();
    //        }

    //        var count = cmbBatchMethod.Items.Count;
    //        cmbBatchMethod.Enabled = count > 0;
    //        if (count > 0)
    //            cmbBatchMethod.SelectedIndex = 0;

    //        cmbBatchMethod.SelectedIndexChanged += CmbBatchMethod_SelectedIndexChanged;
    //    }

    //    private void SetCipherMethods()
    //    {
    //        var variant = _selectedBatchVariant?.Value as ICipherAdapter;
    //        if (variant == null)
    //            return;

    //        var ignoreDecryption = variant.GetType().
    //                                   GetCustomAttributes(typeof(IgnoreDecryptionAttribute), false).
    //                                   Any();
    //        var ignoreEncryption = variant.GetType().
    //                                   GetCustomAttributes(typeof(IgnoreEncryptionAttribute), false).
    //                                   Any();

    //        if (!ignoreDecryption)
    //            cmbBatchMethod.Items.Add(new ComboBoxElement(
    //                null, nameof(ICipherAdapter.Decrypt)));
    //        if (!ignoreEncryption)
    //            cmbBatchMethod.Items.Add(new ComboBoxElement(
    //                null, nameof(ICipherAdapter.Encrypt)));
    //    }

    //    private void SetCompressionMethods()
    //    {
    //        var variant = _selectedBatchVariant?.Value as ICompressionAdapter;
    //        if (variant == null)
    //            return;

    //        var ignoreDecompression = variant.GetType().
    //            GetCustomAttributes(typeof(IgnoreDecompressionAttribute), false).
    //            Any();
    //        var ignoreCompression = variant.GetType().
    //            GetCustomAttributes(typeof(IgnoreCompressionAttribute), false).
    //            Any();

    //        if (!ignoreDecompression)
    //            cmbBatchMethod.Items.Add(new ComboBoxElement(
    //                null, nameof(ICompressionAdapter.Decompress)));
    //        if (!ignoreCompression)
    //            cmbBatchMethod.Items.Add(new ComboBoxElement(
    //                null, nameof(ICompressionAdapter.Compress)));
    //    }

    //    private void SetHashMethods()
    //    {
    //        var variant = _selectedBatchVariant?.Value as IHashAdapter;
    //        if (variant == null)
    //            return;

    //        cmbBatchMethod.Items.Add(new ComboBoxElement(
    //            null, nameof(IHashAdapter.Compute)));
    //    }

    //    private void BtnBrowseInput_Click(object sender, EventArgs e)
    //    {
    //        var fbd = new FolderBrowserDialog
    //        {
    //            Description = "Select the directory to batch through.",
    //            SelectedPath = Settings.Default.BatchInputDirectory
    //        };

    //        if (fbd.ShowDialog() != DialogResult.OK)
    //            return;

    //        txtBatchInputDirectory.Text = fbd.SelectedPath;

    //        Settings.Default.BatchInputDirectory = fbd.SelectedPath;
    //        Settings.Default.Save();
    //    }

    //    private void BtnBrowseOutput_Click(object sender, EventArgs e)
    //    {
    //        var fbd = new FolderBrowserDialog
    //        {
    //            Description = "Select the directory to batch into.",
    //            SelectedPath = Settings.Default.BatchOutputDirectory
    //        };

    //        if (fbd.ShowDialog() != DialogResult.OK)
    //            return;

    //        txtBatchOutputDirectory.Text = fbd.SelectedPath;

    //        Settings.Default.BatchOutputDirectory = fbd.SelectedPath;
    //        Settings.Default.Save();
    //    }

    //    private async void BtnBatchProcess_Click(object sender, EventArgs e)
    //    {
    //        if (string.IsNullOrEmpty(txtBatchInputDirectory.Text))
    //        {
    //            MessageBox.Show("Choose an input directory.", "Missing paths", MessageBoxButtons.OK,
    //                MessageBoxIcon.Information);
    //            return;
    //        }

    //        if ((Type)_selectedBatchType.Value == typeof(IHashAdapter) &&
    //            string.IsNullOrEmpty(txtBatchOutputDirectory.Text))
    //        {
    //            MessageBox.Show("Choose an output directory.", "Missing paths", MessageBoxButtons.OK,
    //                MessageBoxIcon.Information);
    //            return;
    //        }

    //        if (_selectedBatchVariant?.Value == null)
    //            return;

    //        btnBatchProcess.Enabled = false;
    //        txtTaskCount.Enabled = false;
    //        chkSubDirectories.Enabled = false;
    //        cmbBatchType.Enabled = false;
    //        cmbBatchVariant.Enabled = false;
    //        cmbBatchMethod.Enabled = false;

    //        var batchProcessor = new BatchProcessor();
    //        batchProcessor.SearchSubDirectories = chkSubDirectories.Checked;
    //        if (int.TryParse(txtTaskCount.Text, out var taskCount))
    //            batchProcessor.TaskCount = taskCount;

    //        var progress = new Progress<ProgressReport>();
    //        if ((Type)_selectedBatchType.Value == typeof(ICipherAdapter))
    //        {
    //            var adapter = _selectedBatchVariant.Value as ICipherAdapter;
    //            if (adapter == null)
    //                return;

    //            var processor = _selectedBatchMethod.Name == nameof(ICipherAdapter.Decrypt) ?
    //                (IBatchProcessor)new DecryptProcessor(adapter) :
    //                (IBatchProcessor)new EncryptProcessor(adapter);

    //            adapter.RequestData += CipherAdapter_RequestData;

    //            await batchProcessor.Process(txtBatchInputDirectory.Text, txtBatchOutputDirectory.Text,
    //                processor, progress);

    //            adapter.RequestData -= CipherAdapter_RequestData;

    //            _cipherRequestedData = new List<string>();
    //            _cipherRequestTracking = new Dictionary<string, int>();
    //        }
    //        //else if((Type)_selectedBatchType.Value == typeof(ICompressionAdapter))
    //        //{

    //        //}
    //        else if ((Type)_selectedBatchType.Value == typeof(IHashAdapter))
    //        {
    //            var adapter = _selectedBatchVariant.Value as IHashAdapter;
    //            if (adapter == null)
    //                return;

    //            var processor = new ComputeHashProcessor(adapter);
    //            var batchHashProcessor=new BatchHashProcessor();
    //            batchHashProcessor.SearchSubDirectories = chkSubDirectories.Checked;
    //            if (int.TryParse(txtTaskCount.Text, out taskCount))
    //                batchHashProcessor.TaskCount = taskCount;

    //            lstHash.Items.Clear();
    //            var hashResults = await batchHashProcessor.Process(txtBatchInputDirectory.Text, processor, progress);
    //            foreach (var hashResult in hashResults)
    //            {
    //                lstHash.Items.Add(new ListViewItem(new[]
    //                    {hashResult.File, $"0x{hashResult.Result.Aggregate("", (a, b) => a + b.ToString("X2"))}"}));
    //            }
    //        }

    //        btnBatchProcess.Enabled = true;
    //        txtTaskCount.Enabled = true;
    //        chkSubDirectories.Enabled = true;
    //        cmbBatchType.Enabled = true;
    //        cmbBatchVariant.Enabled = true;
    //        cmbBatchMethod.Enabled = true;
    //    }

    //    private List<string> _cipherRequestedData = new List<string>();
    //    private Dictionary<string, int> _cipherRequestTracking = new Dictionary<string, int>();
    //    private static object _cipherLock = new object();
    //    private void CipherAdapter_RequestData(object sender, Kontract.Models.Intermediate.RequestDataEventArgs e)
    //    {
    //        lock (_cipherLock)
    //        {
    //            if (!_cipherRequestTracking.ContainsKey(e.RequestId))
    //                _cipherRequestTracking.Add(e.RequestId, 0);
    //            if (_cipherRequestedData.Count > _cipherRequestTracking[e.RequestId])
    //            {
    //                e.Data = _cipherRequestedData[_cipherRequestTracking[e.RequestId]];
    //                _cipherRequestTracking[e.RequestId]++;
    //                return;
    //            }

    //            var input = new InputBox("Requesting data", e.RequestMessage);
    //            var ofd = new OpenFileDialog() { Title = e.RequestMessage };

    //            while (true)
    //            {
    //                if (e.IsRequestFile)
    //                {
    //                    if (ofd.ShowDialog() == DialogResult.OK && ofd.CheckFileExists)
    //                    {
    //                        e.Data = ofd.FileName;
    //                        _cipherRequestedData.Add(ofd.FileName);
    //                        _cipherRequestTracking[e.RequestId]++;
    //                        return;
    //                    }

    //                    MessageBox.Show("No valid file selected. Please choose a valid file.", "Invalid file",
    //                        MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                }
    //                else
    //                {
    //                    if (input.ShowDialog() == DialogResult.OK && input.InputText.Length == e.DataSize)
    //                    {
    //                        e.Data = input.InputText;
    //                        _cipherRequestedData.Add(input.InputText);
    //                        _cipherRequestTracking[e.RequestId]++;
    //                        return;
    //                    }

    //                    MessageBox.Show("No valid data input. Please input valid data.", "Invalid data",
    //                        MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                }
    //            }
    //        }
    //    }

    //    private void CmbBatchMethod_SelectedIndexChanged(object sender, EventArgs e)
    //    {

    //    }

    //    private void CmbBatchType_SelectedIndexChanged(object sender, EventArgs e)
    //    {
    //        UpdateBatchVariants();
    //        UpdateBatchMethods();
    //        UpdateForm();
    //    }

    //    private void CmbBatchVariant_SelectedIndexChanged(object sender, EventArgs e)
    //    {

    //    }

    //    private void TxtTaskCount_KeyPress(object sender, KeyPressEventArgs e)
    //    {
    //        if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
    //            e.Handled = true;
    //    }
    //}
}
