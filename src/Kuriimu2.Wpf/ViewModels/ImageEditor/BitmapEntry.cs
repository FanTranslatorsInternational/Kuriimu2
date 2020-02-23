using System.Windows.Media.Imaging;
using Kuriimu2.Wpf.Tools;

namespace Kuriimu2.Wpf.ViewModels.ImageEditor
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BitmapEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public ImageInfo ImageInfo = null;

        /// <summary>
        /// 
        /// </summary>
        public string Name => ImageInfo?.Name;

        /// <summary>
        /// 
        /// </summary>
        public BitmapImage Thumbnail => ImageInfo?.Image.ToBitmapImage(true);

        //public BitmapImage ImageTwo => ImageInfo?.MipMaps.Skip(0).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageTwoVisible => ImageInfo?.MipMaps.Count > 0 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageThree => ImageInfo?.MipMaps.Skip(1).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageThreeVisible => ImageInfo?.MipMaps.Count > 1 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageFour => ImageInfo?.MipMaps.Skip(2).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageFourVisible => ImageInfo?.MipMaps.Count > 2 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageFive => ImageInfo?.MipMaps.Skip(3).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageFiveVisible => ImageInfo?.MipMaps.Count > 3 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageSix => ImageInfo?.MipMaps.Skip(4).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageSixVisible => ImageInfo?.MipMaps.Count > 4 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageSeven => ImageInfo?.MipMaps.Skip(5).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageSevenVisible => ImageInfo?.MipMaps.Count > 5 ? Visibility.Visible : Visibility.Hidden;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageInfo"></param>
        public BitmapEntry(ImageInfo imageInfo) => ImageInfo = imageInfo;
    }
}
