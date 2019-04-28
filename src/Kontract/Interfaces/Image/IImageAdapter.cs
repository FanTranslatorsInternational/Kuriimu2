using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Image
{
    /// <inheritdoc cref="IPlugin"/>
    /// <summary>
    /// This is the image adapter interface for creating image format plugins.
    /// </summary>
    public interface IImageAdapter : IPlugin
    {
        /// <summary>
        /// The list of images provided by the image adapter to the UI.
        /// </summary>
        IList<BitmapInfo> BitmapInfos { get; }

        /// <summary>
        /// The list of formats provided by the image adapter to change encoding
        /// </summary>
        IList<FormatInfo> FormatInfos { get; }

        /// <summary>
        /// Instructs the plugin to encode the bitmaps and report progress as it goes.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> to be encoded.</param>
        /// <param name="formatInfo">The <see cref="FormatInfo"/> to encode into.</param>
        /// <param name="progress">The <see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>True if the bitmaps were successfully encoded, False otherwise.</returns>
        Task<bool> Encode(BitmapInfo bitmapInfo, FormatInfo formatInfo, IProgress<ProgressReport> progress);
    }
}
