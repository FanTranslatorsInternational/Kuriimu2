using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore.Generators;
using Kuriimu2.Dialogs.Common;
using Kuriimu2.Tools;
using Microsoft.Win32;
using Action = System.Action;
using FontFamily = System.Drawing.FontFamily;

namespace Kuriimu2.Dialogs.ViewModels
{
    public sealed class BitmapFontGeneratorViewModel : Screen
    {
        private const string _profileFilter = "Bitmap Font Generator Profile (*.bfgp)|*.bfgp";

        private ImageSource _previewCharacterImage;
        private int _marginLeft = 0;
        private int _marginTop = 0;
        private int _marginRight = 0;
        private int _marginBottom = 0;
        private int _paddingLeft = 0;
        private int _paddingTop = 0;
        private int _paddingRight = 0;
        private char _previewCharacter = 'A';
        private string _fontFamily = "Arial";
        private float _fontSize = 24;
        private bool _bold = false;
        private bool _italic = false;
        private int _glyphHeight = 36;

        public BitmapImage Icon { get; }
        public string Error { get; set; } = string.Empty;
        public IFontAdapter Adapter { get; set; } = null;

        public ImageSource PreviewCharacterImage
        {
            get => _previewCharacterImage;
            set
            {
                _previewCharacterImage = value;
                NotifyOfPropertyChange(() => PreviewCharacterImage);
            }
        }

        #region Margin

        public int MarginLeft
        {
            get => _marginLeft;
            set
            {
                if (value == _marginLeft) return;
                _marginLeft = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => MarginLeft);
            }
        }

        public int MarginTop
        {
            get => _marginTop;
            set
            {
                if (value == _marginTop) return;
                _marginTop = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => MarginTop);
            }
        }

        public int MarginRight
        {
            get => _marginRight;
            set
            {
                if (value == _marginRight) return;
                _marginRight = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => MarginRight);
            }
        }

        public int MarginBottom
        {
            get => _marginBottom;
            set
            {
                if (value == _marginBottom) return;
                _marginBottom = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => MarginBottom);
            }
        }

        #endregion

        #region Padding

        public int PaddingLeft
        {
            get => _paddingLeft;
            set
            {
                if (value == _paddingLeft) return;
                _paddingLeft = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => PaddingLeft);
            }
        }

        public int PaddingTop
        {
            get => _paddingTop;
            set
            {
                if (value == _paddingTop) return;
                _paddingTop = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => PaddingTop);
            }
        }

        public int PaddingRight
        {
            get => _paddingRight;
            set
            {
                if (value == _paddingRight) return;
                _paddingRight = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => PaddingRight);
            }
        }

        #endregion

        public char PreviewCharacter
        {
            get => _previewCharacter;
            set
            {
                _previewCharacter = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => PreviewCharacter);
            }
        }

        public List<string> GeneratorTypes => new List<string>
        {
            "GDI+",
            "WPF"
        };

        public string Generator { get; set; } = "GDI+";

        #region Font Settings

        public List<string> FontFamilies => new InstalledFontCollection().Families.Select(ff => ff.Name).ToList();

        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                if (value == _fontFamily) return;
                _fontFamily = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => FontFamily);
            }
        }

        public float FontSize
        {
            get => _fontSize;
            set
            {
                if (value.Equals(_fontSize)) return;
                _fontSize = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => FontSize);
            }
        }

        public bool Bold
        {
            get => _bold;
            set
            {
                if (value == _bold) return;
                _bold = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => Bold);
            }
        }

        public bool Italic
        {
            get => _italic;
            set
            {
                if (value == _italic) return;
                _italic = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => Italic);
            }
        }

        public string Characters { get; set; }

        public int GlyphHeight
        {
            get => _glyphHeight;
            set
            {
                if (value == _glyphHeight) return;
                _glyphHeight = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => GlyphHeight);
            }
        }

        public int CanvasWidth { get; set; } = 1024;
        public int CanvasHeight { get; set; } = 512;
        public bool ShowDebugBoxes { get; set; } = false;

        #endregion

        public Func<ValidationResult> ValidationCallback;
        public Action GenerationCompleteCallback;

        // Constructor
        public BitmapFontGeneratorViewModel()
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/icon-text-page.png"));
            UpdatePreview();
        }

        public void GenerateButton()
        {
            var stop = false;

            if (Characters.Length == 0)
            {
                stop = true;
                Error = "Please provide some characters to generate.";
                NotifyOfPropertyChange(() => Error);
            }
            else
            {
                Error = string.Empty;
                NotifyOfPropertyChange(() => Error);
            }

            if (!stop && ValidationCallback != null)
            {
                var results = ValidationCallback?.Invoke();

                if (!results.CanClose)
                {
                    stop = true;
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
                else
                {
                    Error = string.Empty;
                    NotifyOfPropertyChange(() => Error);
                }
            }

            if (stop) return;

            // Generate
            var fs = FontStyle.Regular;

            if (Bold)
                fs ^= FontStyle.Bold;
            if (Italic)
                fs ^= FontStyle.Italic;

            var ff = new FontFamily(FontFamily);

            if (!ff.IsStyleAvailable(FontStyle.Regular) && (fs & FontStyle.Regular) != 0)
                fs ^= FontStyle.Regular;
            if (!ff.IsStyleAvailable(FontStyle.Bold) && (fs & FontStyle.Bold) != 0)
                fs ^= FontStyle.Bold;
            if (!ff.IsStyleAvailable(FontStyle.Italic) && (fs & FontStyle.Italic) != 0)
                fs ^= FontStyle.Italic;

            var bfg = new Kore.Generators.BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
                Font = new Font(ff, FontSize, fs),
                GlyphMargin = new Kore.Generators.Padding
                {
                    Left = MarginLeft,
                    Top = MarginTop,
                    Right = MarginRight,
                    Bottom = MarginBottom
                },
                GlyphPadding = new Kore.Generators.Padding
                {
                    Left = PaddingLeft,
                    Top = PaddingTop,
                    Right = PaddingRight
                },
                GlyphHeight = GlyphHeight,
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                ShowDebugBoxes = ShowDebugBoxes
            };

            // Select distinct characters amd sort them.
            var chars = Characters.Distinct().Select(c => (ushort)c).ToList();
            chars.Sort();
            bfg.Generate(chars);

            // Update input characters.
            Characters = chars.Aggregate("", (i, o) => i += (char)o);
            NotifyOfPropertyChange(() => Characters);

            GenerationCompleteCallback?.Invoke();
        }

        public void UpdatePreview()
        {
            // Generate
            var fs = FontStyle.Regular;

            if (Bold)
                fs ^= FontStyle.Bold;
            if (Italic)
                fs ^= FontStyle.Italic;

            var ff = new FontFamily(FontFamily);

            if (!ff.IsStyleAvailable(FontStyle.Regular) && (fs & FontStyle.Regular) != 0)
                fs ^= FontStyle.Regular;
            if (!ff.IsStyleAvailable(FontStyle.Bold) && (fs & FontStyle.Bold) != 0)
                fs ^= FontStyle.Bold;
            if (!ff.IsStyleAvailable(FontStyle.Italic) && (fs & FontStyle.Italic) != 0)
                fs ^= FontStyle.Italic;

            var bfg = new Kore.Generators.BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
                Font = new Font(ff, FontSize, fs),
                GlyphMargin = new Kore.Generators.Padding
                {
                    Left = MarginLeft,
                    Top = MarginTop,
                    Right = MarginRight,
                    Bottom = MarginBottom
                },
                GlyphPadding = new Kore.Generators.Padding
                {
                    Left = PaddingLeft,
                    Top = PaddingTop,
                    Right = PaddingRight
                },
                GlyphHeight = GlyphHeight,
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                ShowDebugBoxes = ShowDebugBoxes
            };

            PreviewCharacterImage = bfg.Preview(_previewCharacter).ToBitmapImage();
        }

        public void LoadProfileButton()
        {
            var ofd = new OpenFileDialog {FileName = "", Filter = _profileFilter};
            if (ofd.ShowDialog() != true) return;

            var profile = BitmapFontGeneratorGdiProfile.Load(ofd.FileName);
            
            FontFamily = profile.FontFamily;
            FontSize = profile.FontSize;
            Bold = profile.Bold;
            Italic = profile.Italic;

            MarginLeft = profile.GlyphMargin.Left;
            MarginTop = profile.GlyphMargin.Top;
            MarginRight = profile.GlyphMargin.Right;
            MarginBottom = profile.GlyphMargin.Bottom;

            PaddingLeft = profile.GlyphPadding.Left;
            PaddingTop = profile.GlyphPadding.Top;
            PaddingRight = profile.GlyphPadding.Right;
            
            GlyphHeight = GlyphHeight;
            CanvasWidth = CanvasWidth;
            CanvasHeight = CanvasHeight;
            ShowDebugBoxes = ShowDebugBoxes;
        }

        public void SaveProfileButton()
        {
            var sfd = new SaveFileDialog { FileName = "", Filter = _profileFilter };
            if (sfd.ShowDialog() != true) return;

            var profile = new BitmapFontGeneratorGdiProfile
            {
                FontFamily = FontFamily,
                FontSize = FontSize,
                Bold = Bold,
                Italic = Italic,
                GlyphMargin = new Padding
                {
                    Left = MarginLeft,
                    Top = MarginTop,
                    Right = MarginRight,
                    Bottom = MarginBottom
                },
                GlyphPadding = new Padding
                {
                    Left = PaddingLeft,
                    Top = PaddingTop,
                    Right = PaddingRight,
                },
                GlyphHeight = GlyphHeight,
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                ShowDebugBoxes = ShowDebugBoxes
            };

            profile.Save(sfd.FileName);
        }

        public void CloseButton()
        {
            TryClose();
        }
    }
}
