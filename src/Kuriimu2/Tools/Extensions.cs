using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kuriimu2.Tools
{
    public static class Extensions
    {
        // TODO: These are a mess and this image manipulation stuff in WPF needs to be unified into a common usage.

        // ToBitmapImage/ImageSource
        public static BitmapImage ToBitmapImage(this Image bitmap)
        {
            if (bitmap == null) return null;

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public static ImageSource ToDrawingImage(this Image bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        // ToBitmap
        public static Bitmap ToBitmap(this BitmapImage bitmapImage)
        {
            var enc = new PngBitmapEncoder();
            using (var ms = new MemoryStream())
            {
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(ms);
                var bitmap = new Bitmap(ms);
                return new Bitmap(bitmap);
            }
        }

        public static Bitmap ToBitmap(this DrawingImage drawingImage, double scale = 1.0)
        {
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.PushTransform(new TranslateTransform(-drawingImage.Drawing.Bounds.X, -drawingImage.Drawing.Bounds.Y));
                drawingContext.DrawDrawing(drawingImage.Drawing);
            }

            var width = drawingImage.Drawing.Bounds.Width * scale;
            var height = drawingImage.Drawing.Bounds.Height * scale;
            var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

            var enc = new PngBitmapEncoder();
            using (var ms = new MemoryStream())
            {
                bitmap.Render(drawingVisual);
                enc.Frames.Add(BitmapFrame.Create(bitmap));
                enc.Save(ms);
                return new Bitmap(ms);
            }
        }
    }

    /// <summary>
    /// Focus gives UIElements the ability to be focused from the view model in an MVVM implementation.
    /// </summary>
    public static class Focus
    {
        /// <summary>
        /// IsFocused dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
            "IsFocused", typeof(bool), typeof(Focus),
            new UIPropertyMetadata(false, null, OnCoerceValue)
        );

        /// <summary>
        /// Gets the IsFocused property value.
        /// </summary>
        /// <param name="obj">The UIElement.</param>
        /// <returns>The value.</returns>
        public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);

        /// <summary>
        /// Sets the IsFocused property value.
        /// </summary>
        /// <param name="obj">The UIElement.</param>
        /// <param name="value">The value.</param>
        public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

        /// <summary>
        /// Coerces the IsFocused value from the UIElement.
        /// </summary>
        /// <param name="obj">The UIElement.</param>
        /// <param name="baseValue">The value.</param>
        /// <returns>The value.</returns>
        private static object OnCoerceValue(DependencyObject obj, object baseValue)
        {
            if ((bool)baseValue)
                ((UIElement)obj).Focus();
            else if (((UIElement)obj).IsFocused)
                Keyboard.ClearFocus();
            return (bool)baseValue;
        }
    }
}
