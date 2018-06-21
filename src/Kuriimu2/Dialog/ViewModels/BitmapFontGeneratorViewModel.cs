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
        public BitmapImage Icon { get; private set; }
        public string Message { get; set; } = "Generation Settings:";
        public string Error { get; set; } = string.Empty;
        public IFontAdapter Adapter { get; set; } = null;

        public List<string> GeneratorTypes => new List<string>()
        {
            "GDI+",
            "WPF"
        };
        public string SelectedGenerator { get; set; } = "GDI+";
        public List<string> FontFamilies => new InstalledFontCollection().Families.Select(ff => ff.Name).ToList();
        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 24;
        public string Characters { get; set; }
        public int GlyphHeight { get; set; } = 48;
        public int PaddingLeft { get; set; } = 0;
        public int PaddingRight { get; set; } = 0;
        public int CanvasWidth { get; set; } = 1024;
        public int CanvasHeight { get; set; } = 512;
        public bool Debug { get; set; } = false;

        public Func<ValidationResult> ValidationCallback;
        public Action GenerationCompleteCallback;

        public BitmapFontGeneratorViewModel()
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/icon-generator.png"));
        }

        public void GenerateButton()
        {
            var stop = false;

            if (ValidationCallback != null)
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

            if (!stop)
            {
                var fontGen = new Kore.Generators.BitmapFontGeneratorGdi
                {
                    Adapter = Adapter,
                    Font = new Font(new FontFamily(FontFamily), FontSize),
                    GlyphHeight = GlyphHeight,
                    GlyphLeftPadding = PaddingLeft,
                    GlyphRightPadding = PaddingRight,
                    MaxCanvasWidth = CanvasWidth,
                    MaxCanvasHeight = CanvasHeight,
                    Debug = Debug
                };

                var chars = Characters.Select(c => (ushort)c).ToList();
                chars.Sort();
                fontGen.Generate(chars);

                GenerationCompleteCallback?.Invoke();
            }
        }

        public void CloseButton()
        {
            TryClose();
        }
    }

    public class ListItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }
}
