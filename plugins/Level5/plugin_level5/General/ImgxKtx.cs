﻿using System;
using System.IO;
using System.Threading.Tasks;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using plugin_level5.Compression;

namespace plugin_level5.General
{
    class ImgxKtx
    {
        private static readonly Guid KtxPluginId = Guid.Parse("d25919cc-ac22-4f4a-94b2-b0f42d1123d4");

        private ImgxHeader _header;
        private Level5CompressionMethod _dataCompressionFormat;

        private IFileState _ktxState;
        private IImageState _imageState;

        public IImageInfo Load(Stream input, IFileManager pluginManager)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<ImgxHeader>();

            // Load KTX
            _imageState = LoadKtx(input, "file.ktx", pluginManager).Result;

            return _imageState.Images[0];
        }

        public void Save(Stream output, IFileManager pluginManager)
        {
            using var bw = new BinaryWriterX(output);

            // Save Ktx
            var saveResult = pluginManager.SaveStream(_ktxState).Result;
            if (!saveResult.IsSuccessful)
                throw new InvalidOperationException(saveResult.Message);

            // Compress saved Ktx to output
            output.Position = _header.tableDataOffset;
            Level5Compressor.Compress(saveResult.SavedStream[0].Stream, output, _dataCompressionFormat);
            var compressedLength = output.Length - _header.tableDataOffset;

            // Align file to 16 bytes
            bw.WriteAlignment();

            // Update header
            _header.imageCount = (byte)(_imageState.Images[0].ImageData.MipMapCount + 1);
            _header.width = (short)_imageState.Images[0].ImageSize.Width;
            _header.height = (short)_imageState.Images[0].ImageSize.Height;
            _header.imgDataSize = (int)compressedLength;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);

            // Close Ktx
            pluginManager.Close(_ktxState);
        }

        private async Task<IImageState> LoadKtx(Stream fileStream, UPath filePath, IFileManager pluginManager)
        {
            var imgData = new SubStream(fileStream, _header.tableDataOffset, _header.imgDataSize);
            _dataCompressionFormat = Level5Compressor.PeekCompressionMethod(imgData);

            var ktxFile = new MemoryStream();
            Level5Compressor.Decompress(imgData, ktxFile);
            ktxFile.Position = 0;

            var loadResult = await pluginManager.LoadFile(new StreamFile(ktxFile, filePath.GetNameWithoutExtension() + ".ktx"), KtxPluginId);
            if (!loadResult.IsSuccessful)
                throw new InvalidOperationException(loadResult.Message);
            if (!(loadResult.LoadedFileState.PluginState is IImageState))
                throw new InvalidOperationException("The embedded KTX version is not supported.");

            _ktxState = loadResult.LoadedFileState;
            return (IImageState)_ktxState.PluginState;
        }
    }
}
