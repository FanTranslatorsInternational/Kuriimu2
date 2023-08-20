using System;
using System.Linq;
using System.Security.Claims;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Models;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas.Interfaces;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ImageForm : Component, IKuriimuForm
    {
        private readonly FormInfo<IImageState> _state;

        private int _selectedImgIndex;

        public ImageForm(FormInfo<IImageState> state)
        {
            _state = state;

            InitializeComponent();

            _formatBox.SelectedItemChanged += _formatBox_SelectedItemChanged;
            _paletteBox.SelectedItemChanged += _paletteBox_SelectedItemChanged;
            _imgList.SelectedItemChanged += _imgList_SelectedItemChanged;
            _saveBtn.Clicked += _saveBtn_Clicked;
            _saveAsBtn.Clicked += _saveAsBtn_Clicked;

            UpdateState();
            UpdateForm();
        }

        #region Events

        private void _formatBox_SelectedItemChanged(object sender, EventArgs e)
        {
            var selectedFormat = ((ComboBox<int>)sender).SelectedItem.Content;

            var selectedImg = GetSelectedImage();
            if (selectedImg?.ImageFormat == selectedFormat)
                return;

            selectedImg?.TranscodeImage(selectedFormat, _state.Progress);
            SetImage(selectedImg, _state.Progress);

            _state.FormCommunicator.Update(true, false);
            UpdateForm();
        }

        private void _paletteBox_SelectedItemChanged(object sender, EventArgs e)
        {
            var selectedFormat = ((ComboBox<int>)sender).SelectedItem.Content;

            var selectedImg = GetSelectedImage();
            if (selectedImg?.PaletteFormat == selectedFormat)
                return;

            selectedImg?.TranscodePalette(selectedFormat, _state.Progress);
            SetImage(selectedImg, _state.Progress);

            _state.FormCommunicator.Update(true, false);
            UpdateForm();
        }

        private void _imgList_SelectedItemChanged(object sender, EventArgs e)
        {
            var imgList= (ImageList)sender;
            var selectedItem = (FormImageListItem)imgList.SelectedItem;

            _selectedImgIndex = imgList.Items.IndexOf(selectedItem);

            SetSelectedImage(selectedItem.ImageInfo, _state.Progress);
        }

        private async void _saveBtn_Clicked(object sender, EventArgs e)
        {
            await _state.FormCommunicator.Save(false);
        }

        private async void _saveAsBtn_Clicked(object sender, EventArgs e)
        {
            await _state.FormCommunicator.Save(true);
        }

        private async void Save(bool saveAs)
        {
            await _state.FormCommunicator.Save(saveAs);

            UpdateState();
            UpdateForm();
        }

        private IImageInfo GetSelectedImage()
        {
            var clampedIndex = Math.Clamp(_selectedImgIndex, 0, _state.PluginState.Images.Count - 1);
            return _state.PluginState.Images[clampedIndex];
        }

        private void UpdateState()
        {
            SetImages(_state.PluginState.Images, _state.Progress);
            SetSelectedImage(GetSelectedImage(), _state.Progress);
        }

        #endregion

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);
        }

        #endregion

        #region IKuriimuForm implementation

        public void UpdateForm()
        {
            // Update save button enablement
            var canSave = _state.FileState.PluginState.CanSave;

            _saveBtn.Enabled = canSave && _state.FileState.StateChanged;
            _saveAsBtn.Enabled = canSave && _state.FileState.StateChanged && _state.FileState.ParentFileState == null;
        }

        public bool HasRunningOperations()
        {
            return false;
        }

        public void CancelOperations()
        {
        }

        #endregion
    }
}
