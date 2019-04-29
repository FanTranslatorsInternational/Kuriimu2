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
        /// The list of formats provided by the image adapter to change encoding of the image data.
        /// </summary>
        IList<EncodingInfo> ImageEncodingInfos { get; }

        /// <summary>
        /// Instructs the plugin to encode the bitmaps and report progress as it goes.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> to be encoded.</param>
        /// <param name="encodingInfo">The <see cref="EncodingInfo"/> to encode into.</param>
        /// <param name="progress">The <see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>True if the bitmaps were successfully encoded, False otherwise.</returns>
        /// <remarks><see cref="EncodingInfo"/> should be updated in the given <see cref="BitmapInfo"/> here.</remarks>
        Task<bool> Encode(BitmapInfo bitmapInfo, EncodingInfo encodingInfo, IProgress<ProgressReport> progress);
    }
}
