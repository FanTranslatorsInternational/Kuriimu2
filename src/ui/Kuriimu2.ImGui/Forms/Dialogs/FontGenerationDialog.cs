using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Extensions;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Models;
using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Konnect.Contract.DataClasses.Management.Font;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Management.Font;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;
using PointF = System.Drawing.PointF;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using SolidBrush = System.Drawing.SolidBrush;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class FontGenerationDialog
    {
        private const int DefaultFontSize_ = 12;
        private const int DefaultBaseline_ = 18;
        private const int DefaultGlyphHeight_ = 32;
        private const int DefaultSpaceWidth_ = 4;
        private const int DefaultPaddingLeft_ = 0;
        private const int DefaultPaddingRight_ = 0;

        private readonly IFontFilePluginState _fontState;
        private readonly FontGenerationType _type;
        private readonly FontProfileManager _profileManager = new();

        private bool _isProfile;
        private FontProfile _profile;

        public FontGenerationDialog(IFontFilePluginState fontState, FontGenerationType type, string? selectedCharacters)
        {
            _fontState = fontState;
            _type = type;

            InitializeComponent(type, selectedCharacters);

            _loadBtn.Clicked += _loadBtn_Clicked;
            _saveBtn.Clicked += _saveBtn_Clicked;
            _executeBtn.Clicked += _executeBtn_Clicked;

            _paddingLeftBox.TextChanged += _paddingLeftBox_TextChanged;
            _paddingRightBox.TextChanged += _paddingRightBox_TextChanged;
            _fontFamilyBox.SelectedItemChanged += _fontFamilyBox_SelectedItemChanged;
            _boldCheckBox.CheckChanged += _boldCheckBox_CheckChanged;
            _italicCheckBox.CheckChanged += _italicCheckBox_CheckChanged;
            _fontSizeBox.TextChanged += _fontSizeBox_TextChanged;
            _baselineBox.TextChanged += _baselineBox_TextChanged;
            _glyphHeightBox.TextChanged += _glyphHeightBox_TextChanged;
            _spaceWidthBox.TextChanged += _spaceWidthBox_TextChanged;
            _characterEditor.CursorPositionChanged += _characterEditor_CursorPositionChanged;
            _replaceCharactersCheck.CheckChanged += _replaceCharactersCheck_CheckChanged;

            SetFontFamilies();

            _profile = new FontProfile
            {
                FontFamily = _fontFamilyBox.SelectedItem.Name,
                FontSize = DefaultFontSize_,
                Baseline = DefaultBaseline_,
                GlyphHeight = DefaultGlyphHeight_,
                SpaceWidth = DefaultSpaceWidth_,
                Characters = LocalizationResources.FontGenerateDefaultCharacters,
                Paddings = new Dictionary<char, (int, int)>()
            };
        }

        private void _replaceCharactersCheck_CheckChanged(object? sender, EventArgs e)
        {
            SettingsResources.ReplaceFontCharacters = _replaceCharactersCheck.Checked;
        }

        protected override void ShowInternal()
        {
            switch (Style.Theme)
            {
                case Theme.Dark:
                    var palette = TextEditor.GetDarkPalette();
                    palette[14] = SixLabors.ImageSharp.Color.White.ToUInt32();
                    _characterEditor.SetPalette(palette);
                    break;

                case Theme.Light:
                    var palette1 = TextEditor.GetLightPalette();
                    palette1[14] = TextEditor.GetDarkPalette()[14];
                    _characterEditor.SetPalette(palette1);
                    break;
            }

            if (TryGetCurrentCharacter(out char character))
                SetCurrentCharacter(character);
        }

        protected override Task<bool> ShouldCancelClose()
        {
            return Task.FromResult(_isProfile);
        }

        private void SetFontFamilies()
        {
            _fontFamilyBox.Items.Clear();

            foreach (FontFamily fontFamily in FontFamily.Families)
                _fontFamilyBox.Items.Add(new DropDownItem<FontFamily>(fontFamily, fontFamily.Name));

            if (_fontFamilyBox.Items.Count > 0)
                _fontFamilyBox.SelectedItem = _fontFamilyBox.Items[0];
        }

        private bool TryGetCurrentCharacter(out char character)
        {
            character = '\0';

            var text = _characterEditor.GetText(_characterEditor.GetCursorPosition());
            if (text.Length <= 0)
                return false;

            character = text[0];
            return true;
        }

        private async void _loadBtn_Clicked(object sender, EventArgs e)
        {
            ToggleForm(false);

            var ofd = new WindowsOpenFileDialog
            {
                Title = LocalizationResources.DialogFontGenerateLoadCaption,
                Filters = new List<FileFilter>
                {
                    new(LocalizationResources.DialogFontGenerateProfile, "bfgp")
                }
            };

            DialogResult result = await ofd.ShowAsync();
            if (result == DialogResult.Ok)
                LoadProfile(ofd.Files[0]);

            ToggleForm(true);
        }

        private async void _saveBtn_Clicked(object sender, EventArgs e)
        {
            ToggleForm(false);

            var sfd = new WindowsSaveFileDialog
            {
                Title = LocalizationResources.DialogFontGenerateSaveCaption,
                Filters = new List<FileFilter>
                {
                    new(LocalizationResources.DialogFontGenerateProfile, "bfgp")
                }
            };

            DialogResult result = await sfd.ShowAsync();
            if (result == DialogResult.Ok)
                _profileManager.Save(sfd.Files[0], _profile);

            ToggleForm(true);
        }

        private void _executeBtn_Clicked(object? sender, EventArgs e)
        {
            if (_type == FontGenerationType.Create)
                GenerateFont();
            else if (_type == FontGenerationType.Edit)
                EditFont();
        }

        private void GenerateFont()
        {
            _fontState.AttemptRemoveAll();

            Font font = GetFont();
            foreach (char character in _characterEditor.GetText().Distinct().Order())
            {
                if (char.IsWhiteSpace(character) && _profile.SpaceWidth <= 0)
                    continue;

                CharacterInfo? characterInfo = _fontState.AttemptCreateCharacterInfo(character);
                if (characterInfo is null)
                    continue;

                if (char.IsWhiteSpace(character))
                {
                    characterInfo.BoundingBox = new SixLabors.ImageSharp.Size(_profile.SpaceWidth, _profile.GlyphHeight);
                    characterInfo.GlyphPosition = Point.Empty;
                    characterInfo.Glyph = null;
                }
                else
                {
                    PaddedGlyph? paddedGlyph = GetPaddedGlyph(character, font);

                    if (paddedGlyph is null)
                        continue;

                    characterInfo.BoundingBox = paddedGlyph.BoundingBox;
                    characterInfo.GlyphPosition = paddedGlyph.GlyphPosition;
                    characterInfo.Glyph = paddedGlyph.Glyph;
                }

                characterInfo.ContentChanged = true;

                _fontState.AttemptAddCharacter(characterInfo);
            }

            Close(DialogResult.Ok);
        }

        private void EditFont()
        {
            Font font = GetFont();
            foreach (char character in _characterEditor.GetText().Distinct().Order())
            {
                if (char.IsWhiteSpace(character) && _profile.SpaceWidth <= 0)
                    continue;

                CharacterInfo? characterInfo = _fontState.Characters.FirstOrDefault(c => c.CodePoint == character);
                bool isNew = characterInfo is null;

                if (characterInfo is null)
                {
                    if (!_fontState.CanAddCharacter)
                        continue;

                    characterInfo = _fontState.AttemptCreateCharacterInfo(character);
                    if (characterInfo is null)
                        continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    characterInfo.BoundingBox = new SixLabors.ImageSharp.Size(_profile.SpaceWidth, _profile.GlyphHeight);
                    characterInfo.GlyphPosition = Point.Empty;
                    characterInfo.Glyph = null;
                }
                else
                {
                    PaddedGlyph? paddedGlyph = GetPaddedGlyph(character, font);

                    if (paddedGlyph is null)
                        continue;

                    characterInfo.BoundingBox = paddedGlyph.BoundingBox;
                    characterInfo.GlyphPosition = paddedGlyph.GlyphPosition;
                    characterInfo.Glyph = paddedGlyph.Glyph;
                }

                characterInfo.ContentChanged = true;

                if (isNew)
                    _fontState.AttemptAddCharacter(characterInfo);
            }

            Close(DialogResult.Ok);
        }

        private void _paddingRightBox_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(_paddingRightBox.Text, out int paddingRight))
                return;

            if (!TryGetCurrentCharacter(out char character))
                return;

            if (!_profile.Paddings.TryGetValue(character, out (int, int) padding))
                _profile.Paddings[character] = padding = (DefaultPaddingLeft_, DefaultPaddingRight_);

            _profile.Paddings[character] = (padding.Item1, paddingRight);

            UpdateCurrentGlyph();
        }

        private void _paddingLeftBox_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(_paddingLeftBox.Text, out int paddingLeft))
                return;

            if (!TryGetCurrentCharacter(out char character))
                return;

            if (!_profile.Paddings.TryGetValue(character, out (int, int) padding))
                _profile.Paddings[character] = padding = (DefaultPaddingLeft_, DefaultPaddingRight_);

            _profile.Paddings[character] = (paddingLeft, padding.Item2);

            UpdateCurrentGlyph();
        }

        private void _characterEditor_CursorPositionChanged(object sender, Coordinate e)
        {
            var text = _characterEditor.GetText(e);
            if (text.Length <= 0)
                return;

            var end = _characterEditor.AdvanceCoordinate(e, 1);
            _characterEditor.SetSelection(e, end);

            var character = text[0];
            SetCurrentCharacter(character);
        }

        private void _fontFamilyBox_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_fontFamilyBox.SelectedItem != null)
                _profile.FontFamily = _fontFamilyBox.SelectedItem.Name;

            UpdateCurrentGlyph();
        }

        private void SetFontFamily(string fontName)
        {
            _fontFamilyBox.SelectedItemChanged -= _fontFamilyBox_SelectedItemChanged;

            DropDownItem<FontFamily> fontFamily = _fontFamilyBox.Items.FirstOrDefault(x => x.Name == fontName);
            if (fontFamily != null)
                _fontFamilyBox.SelectedItem = fontFamily;

            _fontFamilyBox.SelectedItemChanged += _fontFamilyBox_SelectedItemChanged;
        }

        private void _italicCheckBox_CheckChanged(object? sender, EventArgs e)
        {
            _profile.IsItalic = _italicCheckBox.Checked;

            UpdateCurrentGlyph();
        }

        private void SetItalic(bool isItalic)
        {
            _italicCheckBox.CheckChanged -= _italicCheckBox_CheckChanged;

            _italicCheckBox.Checked = isItalic;

            _italicCheckBox.CheckChanged += _italicCheckBox_CheckChanged;
        }

        private void _boldCheckBox_CheckChanged(object? sender, EventArgs e)
        {
            _profile.IsBold = _boldCheckBox.Checked;

            UpdateCurrentGlyph();
        }

        private void SetBold(bool isBold)
        {
            _boldCheckBox.CheckChanged -= _boldCheckBox_CheckChanged;

            _boldCheckBox.Checked = isBold;

            _boldCheckBox.CheckChanged += _boldCheckBox_CheckChanged;
        }

        private void _fontSizeBox_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(_fontSizeBox.Text, out int fontSize))
                _profile.FontSize = fontSize;

            UpdateCurrentGlyph();
        }

        private void SetFontSize(int fontSize)
        {
            _fontSizeBox.TextChanged -= _fontSizeBox_TextChanged;

            _fontSizeBox.Text = $"{fontSize}";

            _fontSizeBox.TextChanged += _fontSizeBox_TextChanged;
        }

        private void _baselineBox_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(_baselineBox.Text, out int baseline))
                _profile.Baseline = baseline;

            UpdateCurrentGlyph();
        }

        private void SetBaseline(int baseline)
        {
            _baselineBox.TextChanged -= _baselineBox_TextChanged;

            _baselineBox.Text = $"{baseline}";

            _baselineBox.TextChanged += _baselineBox_TextChanged;
        }

        private void _glyphHeightBox_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(_glyphHeightBox.Text, out int glyphHeight))
                _profile.GlyphHeight = glyphHeight;

            UpdateCurrentGlyph();
        }

        private void SetGlyphHeight(int glyphHeight)
        {
            _glyphHeightBox.TextChanged -= _glyphHeightBox_TextChanged;

            _glyphHeightBox.Text = $"{glyphHeight}";

            _glyphHeightBox.TextChanged += _glyphHeightBox_TextChanged;
        }

        private void _spaceWidthBox_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(_spaceWidthBox.Text, out int spaceWidth))
                _profile.SpaceWidth = spaceWidth;
        }

        private void SetSpaceWidth(int spaceWidth)
        {
            _spaceWidthBox.TextChanged -= _spaceWidthBox_TextChanged;

            _spaceWidthBox.Text = $"{spaceWidth}";

            _spaceWidthBox.TextChanged += _spaceWidthBox_TextChanged;
        }

        private void SetCurrentCharacter(char character)
        {
            if (!_profile.Paddings.TryGetValue(character, out (int, int) padding))
                _profile.Paddings[character] = padding = (DefaultPaddingLeft_, DefaultPaddingRight_);

            SetPaddingTexts(padding);

            UpdateGlyph(character);
        }

        private void SetPaddingTexts((int, int) padding)
        {
            _paddingLeftBox.TextChanged -= _paddingLeftBox_TextChanged;
            _paddingRightBox.TextChanged -= _paddingRightBox_TextChanged;

            _paddingLeftBox.Text = $"{padding.Item1}";
            _paddingRightBox.Text = $"{padding.Item2}";

            _paddingLeftBox.TextChanged += _paddingLeftBox_TextChanged;
            _paddingRightBox.TextChanged += _paddingRightBox_TextChanged;
        }

        private void UpdateCurrentGlyph()
        {
            if (!TryGetCurrentCharacter(out char character))
                return;

            UpdateGlyph(character);
        }

        private void UpdateGlyph(char character)
        {
            var font = GetFont();
            var paddedGlyph = GetPaddedGlyph(character, font);

            _glyphBox.SetPaddedGlyph(paddedGlyph);
        }

        private Font GetFont()
        {
            var fontStyle = FontStyle.Regular;
            if (_profile.IsBold)
                fontStyle |= FontStyle.Bold;
            if (_profile.IsItalic)
                fontStyle |= FontStyle.Italic;

            return new Font(_fontFamilyBox.SelectedItem.Content, _profile.FontSize, fontStyle);
        }

        private PaddedGlyph? GetPaddedGlyph(char character, Font font)
        {
            if (!_profile.Paddings.TryGetValue(character, out (int, int) padding))
                padding = (DefaultPaddingLeft_, DefaultPaddingRight_);

            Image<Rgba32>? glyph = GetGlyph(character, font, out SixLabors.ImageSharp.Size boundingBox, out Point glyphPosition);

            if (glyph is null)
                return null;

            return new PaddedGlyph
            {
                Glyph = glyph,
                BoundingBox = new SixLabors.ImageSharp.Size(boundingBox.Width + padding.Item1 + padding.Item2, boundingBox.Height),
                GlyphPosition = new Point(glyphPosition.X + padding.Item1, glyphPosition.Y),
                Baseline = _profile.Baseline
            };
        }

        private Image<Rgba32>? GetGlyph(char character, Font font, out SixLabors.ImageSharp.Size boundingBox, out Point glyphPosition)
        {
            boundingBox = SixLabors.ImageSharp.Size.Empty;
            glyphPosition = Point.Empty;

            System.Drawing.Image? glyphImage = GetNativeGlyph(character, font);

            if (glyphImage is null)
                return null;

            using Graphics gfx = Graphics.FromImage(glyphImage);
            float glyphY = _profile.Baseline - gfx.DpiY / 72f *
                (font.SizeInPoints / font.FontFamily.GetEmHeight(font.Style) *
                 font.FontFamily.GetCellAscent(font.Style)) + 0.475f;

            var ms = new MemoryStream();
            glyphImage.Save(ms, ImageFormat.Png);

            ms.Position = 0;
            Image<Rgba32> glyph = Image.Load<Rgba32>(ms);

            GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(glyph);

            boundingBox = new SixLabors.ImageSharp.Size(glyphImage.Width, glyphImage.Height);
            glyphPosition = glyphDescription.Position with { Y = (int)(glyphDescription.Position.Y + glyphY) };

            if (glyphDescription.Size is { Width: > 0, Height: > 0 })
                glyph = glyph.Clone(context => context.Crop(new Rectangle(glyphDescription.Position, glyphDescription.Size)));

            return glyph;
        }

        private System.Drawing.Image? GetNativeGlyph(char character, Font font)
        {
            int measuredWidth = _profile.SpaceWidth;
            if (!char.IsWhiteSpace(character))
                measuredWidth = (int)Math.Ceiling(MeasureCharacter(character, font).Width);

            if (measuredWidth <= 0)
                return null;

            var glyphImage = new Bitmap(measuredWidth, _profile.GlyphHeight);
            using Graphics gfx = Graphics.FromImage(glyphImage);

            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.None;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            gfx.DrawString($"{character}", font, new SolidBrush(Style.Theme == Theme.Dark ? System.Drawing.Color.White : System.Drawing.Color.Black), PointF.Empty, StringFormat.GenericTypographic);

            return glyphImage;
        }

        private System.Drawing.SizeF MeasureCharacter(char character, Font font)
        {
            var gfx = Graphics.FromHwnd(nint.Zero);
            return gfx.MeasureString($"{character}", font, PointF.Empty, StringFormat.GenericTypographic);
        }

        private void ToggleForm(bool toggle)
        {
            _isProfile = !toggle;

            _loadBtn.Enabled = toggle;
            _saveBtn.Enabled = toggle;
            _executeBtn.Enabled = toggle;

            _paddingLeftBox.Enabled = toggle;
            _paddingRightBox.Enabled = toggle;
            _fontFamilyBox.Enabled = toggle;
            _boldCheckBox.Enabled = toggle;
            _italicCheckBox.Enabled = toggle;
            _fontSizeBox.Enabled = toggle;
            _baselineBox.Enabled = toggle;
            _glyphHeightBox.Enabled = toggle;
            _spaceWidthBox.Enabled = toggle;
            _characterEditor.Enabled = toggle;

            _glyphBox.Enabled = toggle;
        }

        private void LoadProfile(string profilePath)
        {
            FontProfile? loadedProfile = _profileManager.Load(profilePath);
            if (loadedProfile == null)
                return;

            _profile = loadedProfile;

            SetFontFamily(_profile.FontFamily);
            SetBold(_profile.IsBold);
            SetItalic(_profile.IsItalic);
            SetFontSize(_profile.FontSize);
            SetBaseline(_profile.Baseline);
            SetGlyphHeight(_profile.GlyphHeight);
            SetSpaceWidth(_profile.SpaceWidth);

            if (_replaceCharactersCheck.Checked && _profile.Characters.Length >= 1)
            {
                _characterEditor.SetText(_profile.Characters);
                SetCurrentCharacter(_profile.Characters[0]);
            }

            UpdateCurrentGlyph();
        }
    }
}
