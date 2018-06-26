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
using Kuriimu2.Dialogs.Common;
using Kuriimu2.Tools;
using Action = System.Action;
using FontFamily = System.Drawing.FontFamily;

namespace Kuriimu2.Dialogs.ViewModels
{
    public sealed class BitmapFontGeneratorViewModel : Screen
    {
        private ImageSource _previewCharacter;
        private int _marginLeft = 0;
        private int _marginTop = 0;
        private int _marginRight = 0;
        private int _marginBottom = 0;
        private int _paddingLeft = 0;
        private int _paddingTop = 0;
        private int _paddingRight = 0;
        private string _fontFamily = "Arial";
        private float _fontSize = 24;
        private bool _bold = false;
        private bool _italic = false;
        private int _glyphHeight = 36;

        public BitmapImage Icon { get; }
        public string Error { get; set; } = string.Empty;
        public IFontAdapter Adapter { get; set; } = null;

        public ImageSource PreviewCharacter
        {
            get => _previewCharacter;
            set
            {
                _previewCharacter = value;
                NotifyOfPropertyChange(() => PreviewCharacter);
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
        public bool Debug { get; set; } = false;

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

            var bfg = new Kore.Generators.BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
                Font = new Font(new FontFamily(FontFamily), FontSize, fs),
                GlyphHeight = GlyphHeight,
                GlyphLeftPadding = PaddingLeft,
                GlyphRightPadding = PaddingRight,
                GlyphTopPadding = PaddingTop,
                MaxCanvasWidth = CanvasWidth,
                MaxCanvasHeight = CanvasHeight,
                Debug = Debug
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

            var bfg = new Kore.Generators.BitmapFontGeneratorGdi
            {
                Adapter = Adapter,
                Font = new Font(new FontFamily(FontFamily), FontSize, fs),
                GlyphHeight = GlyphHeight,
                GlyphLeftPadding = PaddingLeft,
                GlyphRightPadding = PaddingRight,
                GlyphTopPadding = PaddingTop,
                MaxCanvasWidth = CanvasWidth,
                MaxCanvasHeight = CanvasHeight,
                Debug = true
            };

            bfg.Preview('A').Save("C:\\test.png", ImageFormat.Png);

            PreviewCharacter = bfg.Preview('A').ToBitmapImage();
        }

        public void CloseButton()
        {
            TryClose();
        }
    }
}
