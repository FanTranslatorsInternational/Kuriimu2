using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kuriimu2.Dialog.Common;
using Action = System.Action;

namespace Kuriimu2.Dialog.ViewModels
{
    public sealed class BitmapFontGeneratorViewModel : Screen
    {
        public BitmapImage Icon { get; }
        public string Message { get; set; } = "Generation Settings:";
        public string Error { get; set; } = string.Empty;
        public IFontAdapter Adapter { get; set; } = null;

        public List<string> GeneratorTypes => new List<string>
        {
            "GDI+",
            "WPF"
        };
        public string SelectedGenerator { get; set; } = "GDI+";
        public List<string> FontFamilies => new InstalledFontCollection().Families.Select(ff => ff.Name).ToList();
        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 24;
        public bool Bold { get; set; } = false;
        public bool Italic { get; set; } = false;
        public string Characters { get; set; }
        public int GlyphHeight { get; set; } = 36;
        public int PaddingLeft { get; set; } = 0;
        public int PaddingRight { get; set; } = 0;
        public int PaddingTop { get; set; } = 0;
        public int CanvasWidth { get; set; } = 1024;
        public int CanvasHeight { get; set; } = 512;
        public bool Debug { get; set; } = false;

        public Func<ValidationResult> ValidationCallback;
        public Action GenerationCompleteCallback;

        public BitmapFontGeneratorViewModel()
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/icon-text-page.png"));
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

        public void CloseButton()
        {
            TryClose();
        }
    }
}
