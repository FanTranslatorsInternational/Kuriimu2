using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Font;
using Kore.Generators;
using Kuriimu2.Wpf.Tools;
using Action = System.Action;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Pen = System.Drawing.Pen;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Screen = Caliburn.Micro.Screen;
using ValidationResult = Kuriimu2.Wpf.Dialogs.Common.ValidationResult;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    public sealed class BitmapFontGeneratorViewModel : Screen
    {
        private const string _profileFilter = "Bitmap Font Generator Profile (*.bfgp)|*.bfgp";

        private ImageSource _previewCharacterImage;
        private int _paddingLeft;
        private int _paddingRight;
        private char _previewCharacter = 'A';
        private int _zoomLevel = 5;
        private ObservableCollection<AdjustedCharacter> _adjustedCharacters;
        private AdjustedCharacter _selectedAdjustedCharacter;
        private string _fontFamily = "Arial";
        private float _fontSize = 24;
        private float _baseline = 30;
        private int _glyphHeight = 36;
        private bool _bold;
        private bool _italic;
        private string textRenderingHint = "AntiAlias";
        private string _characters;
        private int _caretIndex;
        private float _spaceWidth = 7f;
        private bool _showDebugBoxes = true;

        private string _error;

        public BitmapImage Icon { get; }
        public IFontState State { get; set; } = null;

        public string Error
        {
            get => _error;
            set
            {
                _error = value;
                NotifyOfPropertyChange(() => Error);
            }
        }

        #region Preview

        public ImageSource PreviewCharacterImage
        {
            get => _previewCharacterImage;
            set
            {
                _previewCharacterImage = value;
                NotifyOfPropertyChange(() => PreviewCharacterImage);
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

        #region Adjusted Characters

        public ObservableCollection<AdjustedCharacter> AdjustedCharacters
        {
            get => _adjustedCharacters;
            set
            {
                if (Equals(value, _adjustedCharacters)) return;
                _adjustedCharacters = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => AdjustedCharacters);
            }
        }

        public void AdjustedCharacterUpdated(DataGridCellEditEndingEventArgs eventArgs)
        {
            UpdatePreview();
        }

        public AdjustedCharacter SelectedAdjustedCharacter
        {
            get => _selectedAdjustedCharacter;
            set
            {
                if (Equals(value, _selectedAdjustedCharacter)) return;
                _selectedAdjustedCharacter = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => SelectedAdjustedCharacter);
            }
        }

        #endregion

        #region Font Settings

        public List<string> GeneratorTypes => new List<string>
        {
            "GDI+",
            "WPF"
        };

        public string Generator { get; set; } = "GDI+";

        public List<string> FontFamilies =>
            new InstalledFontCollection().Families.Select(ff => ff.Name).ToList();

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
                if (Math.Abs(value - _baseline) < 0.1) return;

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

        public string Characters
        {
            get => _characters;
            set
            {
                if (value == _characters) return;
                _characters = value;
                NotifyOfPropertyChange(() => Characters);
            }
        }

        public int CaretIndex
        {
            get => _caretIndex;
            set
            {
                if (value == _caretIndex) return;
                _caretIndex = value;
                NotifyOfPropertyChange(() => CaretIndex);
                if (value < Characters.Length)
                    PreviewCharacter = Characters[value];
                else if (value == Characters.Length)
                    PreviewCharacter = Characters.Last();
            }
        }

        public float SpaceWidth
        {
            get => _spaceWidth;
            set
            {
                if (Math.Abs(value - _spaceWidth) < 0.1) return;

                _spaceWidth = value;
                NotifyOfPropertyChange(() => SpaceWidth);
            }
        }

        public List<string> TextRenderingHints =>
            Enum.GetNames(typeof(TextRenderingHint)).ToList();

        public string TextRenderingHint
        {
            get => textRenderingHint;
            set
            {
                if (value == textRenderingHint) return;
                textRenderingHint = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => TextRenderingHint);
            }
        }

        public bool ShowDebugBoxes
        {
            get => _showDebugBoxes;
            set
            {
                if (value == _showDebugBoxes) return;
                _showDebugBoxes = value;
                UpdatePreview();
                NotifyOfPropertyChange(() => ShowDebugBoxes);
            }
        }

        #endregion

        public Func<ValidationResult> ValidationCallback;
        public Action GenerationCompleteCallback;

        /// <summary>
        /// Creates a new instance of <see cref="BitmapFontGeneratorViewModel"/>.
        /// </summary>
        public BitmapFontGeneratorViewModel()
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/icon-text-page.png"));
            AdjustedCharacters = new ObservableCollection<AdjustedCharacter>();
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdatePreview();
            return base.OnActivateAsync(cancellationToken);
        }

        #region Zoom button events

        /// <summary>
        /// Zooms out the preview character.
        /// </summary>
        public void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 1, 1);

        /// <summary>
        /// Zooms in the preview character.
        /// </summary>
        public void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 1, 10);

        #endregion

        #region Adjusted character button events

        /// <summary>
        /// Adds a new padding adjustment for characters.
        /// </summary>
        public void AddAdjustedCharacter()
        {
            if (AdjustedCharacters.Any(ac => ac.Character == PreviewCharacter))
                return;

            AdjustedCharacters.Add(new AdjustedCharacter
            {
                Character = PreviewCharacter,
                Padding = new Padding
                {
                    Left = PaddingLeft,
                    Right = PaddingRight
                }
            });

            NotifyOfPropertyChange(() => AdjustedCharacters);
        }

        /// <summary>
        /// Deletes a padding adjustment for characters.
        /// </summary>
        public void DeleteAdjustedCharacter()
        {
            if (SelectedAdjustedCharacter == null)
                return;

            AdjustedCharacters.Remove(SelectedAdjustedCharacter);
            SelectedAdjustedCharacter = AdjustedCharacters.FirstOrDefault();

            NotifyOfPropertyChange(() => AdjustedCharacters);
        }

        #endregion

        #region Main button events

        /// <summary>
        /// Loads the profile for a font.
        /// </summary>
        public void LoadProfileButton()
        {
            var ofd = new OpenFileDialog { FileName = "", Filter = _profileFilter };
            if (ofd.ShowDialog() == false)
                return;

            var profile = BitmapFontGeneratorGdiProfile.Load(ofd.FileName);

            PaddingLeft = profile.GlyphPadding.Left;
            PaddingRight = profile.GlyphPadding.Right;
            AdjustedCharacters = new ObservableCollection<AdjustedCharacter>(profile.AdjustedCharacters);

            FontFamily = profile.FontFamily;
            FontSize = profile.FontSize;

            Baseline = profile.Baseline;
            GlyphHeight = profile.GlyphHeight;

            Bold = profile.Bold;
            Italic = profile.Italic;

            SpaceWidth = profile.SpaceWidth;

            TextRenderingHint = profile.TextRenderingHint;
            Characters = profile.Characters;

            ShowDebugBoxes = profile.ShowDebugBoxes;
        }

        /// <summary>
        /// Saves a profile for a font.
        /// </summary>
        public void SaveProfileButton()
        {
            var sfd = new SaveFileDialog { FileName = "", Filter = _profileFilter };
            if (sfd.ShowDialog() == false)
                return;

            var profile = new BitmapFontGeneratorGdiProfile
            {
                GlyphPadding = new Padding
                {
                    Left = PaddingLeft,
                    Right = PaddingRight
                },
                AdjustedCharacters = AdjustedCharacters.ToList(),

                FontFamily = FontFamily,
                FontSize = FontSize,

                Baseline = Baseline,
                GlyphHeight = GlyphHeight,

                Bold = Bold,
                Italic = Italic,

                SpaceWidth = SpaceWidth,

                TextRenderingHint = TextRenderingHint,
                Characters = Characters,

                ShowDebugBoxes = ShowDebugBoxes
            };

            profile.Save(sfd.FileName);
        }

        /// <summary>
        /// Generates a new set of glyphs
        /// </summary>
        public void GenerateButton()
        {
            Error = string.Empty;

            if (Characters.Length == 0)
            {
                Error = "Please provide some characters to generate.";
                return;
            }

            if (ValidationCallback != null)
            {
                var results = ValidationCallback?.Invoke();

                if (!results.CanClose)
                {
                    Error = results.ErrorMessage;
                    return;
                }
            }

            // Generate glyphs
            var chars = Characters.Distinct().Select(c => c.ToString()).ToArray();
            var glyphs = GenerateGlyphs(chars).ToArray();

            // Set and update new characters
            State.Baseline = Baseline;
            (State as IRemoveCharacters).RemoveAll();

            for (var i = 0; i < chars.Length; i++)
            {
                var newFontCharacter = (State as IAddCharacters).CreateCharacterInfo(Characters[i]);
                newFontCharacter.SetGlyph(glyphs[i]);
                newFontCharacter.SetCharacterSize(new Size(glyphs[i].Width, 0));

                (State as IAddCharacters).AddCharacter(newFontCharacter);
            }

            GenerationCompleteCallback?.Invoke();
        }

        public void CloseButton()
        {
            TryCloseAsync();
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Update preview character.
        /// </summary>
        private void UpdatePreview()
        {
            var font = SetupFont();
            var brush = new SolidBrush(Color.White);

            var padding = new Padding { Left = PaddingLeft, Right = PaddingRight };
            var glyph = GenerateGlyph(_previewCharacter.ToString(), font, brush, padding, ShowDebugBoxes);
            PreviewCharacterImage = glyph.ToBitmapImage();
        }

        /// <summary>
        /// Generates a new set of glyphs.
        /// </summary>
        /// <param name="characters">The characters to generate glyphs from.</param>
        /// <returns>The generated glyphs.</returns>
        private IEnumerable<Bitmap> GenerateGlyphs(string[] characters)
        {
            var font = SetupFont();
            var brush = new SolidBrush(Color.White);

            foreach (var character in characters)
            {
                var padding = _adjustedCharacters.FirstOrDefault(x => x.Character.ToString() == character)?.Padding;
                yield return GenerateGlyph(character, font, brush, padding);
            }
        }

        /// <summary>
        /// Sets up the <see cref="Font"/> to draw a glyph.
        /// </summary>
        /// <returns>The created <see cref="Font"/>.</returns>
        private Font SetupFont()
        {
            var fontFamily = new FontFamily(FontFamily);
            var fontStyle = fontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular :
                fontFamily.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold :
                fontFamily.IsStyleAvailable(FontStyle.Italic) ? FontStyle.Italic :
                FontStyle.Regular;

            if (Bold && fontFamily.IsStyleAvailable(FontStyle.Bold))
                fontStyle ^= FontStyle.Bold;
            if (Italic && fontFamily.IsStyleAvailable(FontStyle.Italic))
                fontStyle ^= FontStyle.Italic;

            return new Font(fontFamily, FontSize, fontStyle);
        }

        /// <summary>
        /// Generates a glyph.
        /// </summary>
        /// <param name="character">The character to draw.</param>
        /// <param name="font">The font to draw the character with.</param>
        /// <param name="brush">The brush to draw the character with.</param>
        /// <param name="padding">The adjusted padding for this character.</param>
        /// <param name="drawDebugBoxes">Should debug boxes be drawn into the glyph.</param>
        /// <returns>The generated glyph.</returns>
        private Bitmap GenerateGlyph(string character, Font font, Brush brush, Padding padding = null, bool drawDebugBoxes = false)
        {
            var measuredWidth = SpaceWidth;
            if (!string.IsNullOrWhiteSpace(character))
                measuredWidth = MeasureCharacter(character, font).Width;

            var glyphWidth = (int)Math.Round(measuredWidth);
            if (padding != null)
                glyphWidth = glyphWidth + padding.Left + padding.Right;

            var glyph = new Bitmap(glyphWidth, GlyphHeight);
            var gfx = Graphics.FromImage(glyph);

            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.None;
            gfx.TextRenderingHint = (TextRenderingHint)Enum.Parse(typeof(TextRenderingHint), TextRenderingHint);

            var baselineOffsetPixels = Baseline - gfx.DpiY / 72f *
                                       (font.SizeInPoints / font.FontFamily.GetEmHeight(font.Style) *
                                        font.FontFamily.GetCellAscent(font.Style));
            var point = new PointF(padding?.Left ?? 0, baselineOffsetPixels + 0.475f);

            if (drawDebugBoxes)
            {
                // Baseline
                gfx.DrawLine(new Pen(Color.FromArgb(100, 255, 255, 255)),
                    new PointF(0, Baseline),
                    new PointF(glyphWidth - 1, Baseline));

                // Padded glyph Box
                gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 255, 0), 1),
                    new Rectangle(0, 0, glyphWidth - 1, GlyphHeight - 1));

                // GlyphInfo Box
                gfx.DrawRectangle(new Pen(Color.FromArgb(127, 255, 0, 0), 1),
                    new Rectangle(padding?.Left ?? 0, 0, (int)Math.Round(measuredWidth) - 1, GlyphHeight - 1));
            }

            gfx.DrawString(character, font, brush, point, StringFormat.GenericTypographic);

            return glyph;
        }

        /// <summary>
        /// Measures the width of a character in the given font.
        /// </summary>
        /// <param name="character">The character to measure.</param>
        /// <param name="font">The font to measure with.</param>
        /// <returns>The size of the character.</returns>
        private SizeF MeasureCharacter(string character, Font font)
        {
            var gfx = Graphics.FromHwnd(IntPtr.Zero);
            return gfx.MeasureString(character, font, PointF.Empty, StringFormat.GenericTypographic);
        }

        #endregion
    }
}
