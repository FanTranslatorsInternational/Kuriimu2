using System.Windows.Media.Imaging;
using Kontract.Interfaces.Image;
using Kontract.Models.Image;
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
        public BitmapInfo BitmapInfo = null;

        /// <summary>
        /// 
        /// </summary>
        public string Name => BitmapInfo?.Name;

        /// <summary>
        /// 
        /// </summary>
        public BitmapImage Thumbnail => BitmapInfo?.Image.ToBitmapImage(true);

        //public BitmapImage ImageTwo => BitmapInfo?.MipMaps.Skip(0).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageTwoVisible => BitmapInfo?.MipMaps.Count > 0 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageThree => BitmapInfo?.MipMaps.Skip(1).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageThreeVisible => BitmapInfo?.MipMaps.Count > 1 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageFour => BitmapInfo?.MipMaps.Skip(2).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageFourVisible => BitmapInfo?.MipMaps.Count > 2 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageFive => BitmapInfo?.MipMaps.Skip(3).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageFiveVisible => BitmapInfo?.MipMaps.Count > 3 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageSix => BitmapInfo?.MipMaps.Skip(4).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageSixVisible => BitmapInfo?.MipMaps.Count > 4 ? Visibility.Visible : Visibility.Hidden;

        //public BitmapImage ImageSeven => BitmapInfo?.MipMaps.Skip(5).FirstOrDefault()?.ToBitmapImage(true);
        //public Visibility ImageSevenVisible => BitmapInfo?.MipMaps.Count > 5 ? Visibility.Visible : Visibility.Hidden;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmapInfo"></param>
        public BitmapEntry(BitmapInfo bitmapInfo) => BitmapInfo = bitmapInfo;
    }
}
