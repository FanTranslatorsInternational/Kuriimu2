﻿using System.Collections.Generic;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Plugins.State
{
    public interface IImageState : IPluginState
    {
        IList<ImageInfo> Images { get; }

        IDictionary<int, IColorEncoding> SupportedEncodings { get; }

        IDictionary<int, (IColorIndexEncoding Encoding, IList<int> PaletteEncodingIndices)> SupportedIndexEncodings { get; }

        IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        ImageInfo ConvertToImageInfo(IndexImageInfo indexImageInfo);

        IndexImageInfo ConvertToIndexImageInfo(ImageInfo imageInfo, int paletteFormat, byte[] paletteData);
    }
}