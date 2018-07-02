using System;
using System.Collections.Generic;
using System.Drawing;
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
        private int _marginLeft;
        private int _marginTop;
        private int _marginRight;
        private int _marginBottom;
        private int _paddingLeft;
        private int _paddingTop;
        private int _paddingRight;
        private char _previewCharacter = 'A';
        private int _zoomLevel = 5;
        private string _fontFamily = "Arial";
        private float _fontSize = 24;
        private float _baseline = 30;
        private int _glyphHeight = 36;
        private bool _bold;
        private bool _italic;

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

        #region Preview

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

        public int ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (value == _zoomLevel) return;
                _zoomLevel = value;
                NotifyOfPropertyChange(() => ZoomLevel);
            }
        }

        #endregion

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

        public float Baseline
        {
            get => _baseline;
            set
            {
                if (value == _baseline) return;
                _baseline = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => Baseline);
            }
        }

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

        public int CanvasWidth { get; set; } = 1024;
        public int CanvasHeight { get; set; } = 512;
        public bool ShowDebugBoxes { get; set; }

        #endregion

        public Func<ValidationResult> ValidationCallback;
        public Action GenerationCompleteCallback;

        // Constructor
        public BitmapFontGeneratorViewModel()
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/icon-text-page.png"));
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            UpdatePreview();
        }

        public void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 1, 1);

        public void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 1, 5);

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
            var ff = new FontFamily(FontFamily);
            var fs = ff.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : ff.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : ff.IsStyleAvailable(FontStyle.Italic) ? FontStyle.Italic : FontStyle.Regular;

            if (Bold && ff.IsStyleAvailable(FontStyle.Bold))
                fs ^= FontStyle.Bold;
            if (Italic && ff.IsStyleAvailable(FontStyle.Italic))
                fs ^= FontStyle.Italic;

            var bfg = new BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
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
                    Right = PaddingRight
                },
                Font = new Font(ff, FontSize, fs),
                Baseline = Baseline,
                GlyphHeight = GlyphHeight,
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                ShowDebugBoxes = ShowDebugBoxes
            };

            // Select distinct characters and sort them.
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
            var ff = new FontFamily(FontFamily);
            var fs = ff.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : ff.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : ff.IsStyleAvailable(FontStyle.Italic) ? FontStyle.Italic : FontStyle.Regular;

            if (Bold && ff.IsStyleAvailable(FontStyle.Bold))
                fs ^= FontStyle.Bold;
            if (Italic && ff.IsStyleAvailable(FontStyle.Italic))
                fs ^= FontStyle.Italic;

            var bfg = new BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
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
                    Right = PaddingRight
                },
                Font = new Font(ff, FontSize, fs),
                Baseline = Baseline,
                GlyphHeight = GlyphHeight,
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                ShowDebugBoxes = ShowDebugBoxes
            };

            PreviewCharacterImage = bfg.Preview(_previewCharacter).ToBitmapImage();
        }

        public void LoadProfileButton()
        {
            var ofd = new OpenFileDialog { FileName = "", Filter = _profileFilter };
            if (ofd.ShowDialog() != true) return;

            var profile = BitmapFontGeneratorGdiProfile.Load(ofd.FileName);

            MarginLeft = profile.GlyphMargin.Left;
            MarginTop = profile.GlyphMargin.Top;
            MarginRight = profile.GlyphMargin.Right;
            MarginBottom = profile.GlyphMargin.Bottom;

            PaddingLeft = profile.GlyphPadding.Left;
            PaddingRight = profile.GlyphPadding.Right;

            FontFamily = profile.FontFamily;
            FontSize = profile.FontSize;
            Baseline = profile.Baseline;
            GlyphHeight = profile.GlyphHeight;
            Bold = profile.Bold;
            Italic = profile.Italic;
            Characters = profile.Characters;
            CanvasWidth = profile.CanvasWidth;
            CanvasHeight = profile.CanvasHeight;
            ShowDebugBoxes = profile.ShowDebugBoxes;
        }

        public void SaveProfileButton()
        {
            var sfd = new SaveFileDialog { FileName = "", Filter = _profileFilter };
            if (sfd.ShowDialog() != true) return;

            var profile = new BitmapFontGeneratorGdiProfile
            {
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
                    Right = PaddingRight
                },
                FontFamily = FontFamily,
                FontSize = FontSize,
                Baseline = Baseline,
                GlyphHeight = GlyphHeight,
                Bold = Bold,
                Italic = Italic,
                Characters = Characters,
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
