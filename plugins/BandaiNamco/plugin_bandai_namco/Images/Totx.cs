using Kanvas;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_bandai_namco.Images
{
    class Totx
    {
        private static readonly Guid CtpkId = Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private IFileState _state;

        public IReadOnlyList<IImageFile> Load(Stream input, IPluginFileManager fileManager)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = ReadHeader(br);

            // Read CTPK
            var ctpkState = fileManager.LoadFile(new StreamFile
            {
                Path = "file.ctpk",
                Stream = new SubStream(input, 0x80, input.Length - 0x80)
            }, CtpkId).Result;

            if (ctpkState.Status is not LoadStatus.Successful)
                throw new InvalidOperationException(ctpkState.Reason.ToString());

            _state = ctpkState.LoadedFileState;
            var images = (_state.PluginState as IImageFilePluginState).Images;

            // Edit image info
            images[0].ImageInfo.ImageSize = new Size(header.width, header.height);
            images[0].ImageInfo.PadSize = (builder => builder.ToMultiple(8));

            return images;
        }

        public void Save(Stream output, IPluginFileManager fileManager)
        {
            var images = (_state.PluginState as IImageFilePluginState).Images;

            using var bw = new BinaryWriterX(output);

            // Create header
            var header = new TotxHeader
            {
                width = (short)images[0].ImageInfo.ImageSize.Width,
                height = (short)images[0].ImageInfo.ImageSize.Height
            };

            // Prepare image info
            var paddedWidth = SizePadding.Multiple(images[0].ImageInfo.ImageSize.Width, 8);
            var paddedHeight = SizePadding.Multiple(images[0].ImageInfo.ImageSize.Height, 8);
            images[0].ImageInfo.ImageSize = new Size(paddedWidth, paddedHeight);

            // Write CTPK
            var ctpkStream = _state.StateChanged ?
                fileManager.SaveStream(_state).Result.SavedStreams[0].Stream :
                _state.FileSystem.OpenFile(_state.FilePath);

            ctpkStream.Position = 0;
            output.Position = 0x80;
            ctpkStream.CopyTo(output);

            // Write header
            output.Position = 0;
            WriteHeader(header, bw);

            // Finalize file manager
            fileManager.Close(_state);
            _state = null;
        }

        private TotxHeader ReadHeader(BinaryReaderX reader)
        {
            return new TotxHeader
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16()
            };
        }

        private void WriteHeader(TotxHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.zero1);
            writer.Write(header.width);
            writer.Write(header.height);
        }
    }
}
