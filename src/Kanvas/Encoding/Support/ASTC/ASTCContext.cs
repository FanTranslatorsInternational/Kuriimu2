using System;
using Kanvas.Encoding.Support.ASTC.Models;

namespace Kanvas.Encoding.Support.ASTC
{
    /// <summary>
    /// Creates a new ASTC context.
    /// </summary>
    internal class ASTCContext : IDisposable
    {
        private IntPtr _context;

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a new ASTC context.
        /// </summary>
        public ASTCContext()
        {
            _context = NativeCalls.CreateContext();
        }

        /// <summary>
        /// Sets the mode to decode the image with.
        /// </summary>
        /// <param name="mode"><see cref="DecodeMode"/></param>
        public void SetDecodeMode(DecodeMode mode)
        {
            NativeCalls.SetDecodeMode(_context, mode);
        }

        /// <summary>
        /// Sets the speed and therefore accuracy of the conversion.
        /// </summary>
        /// <param name="mode"><see cref="SpeedMode"/></param>
        public void SetSpeedMode(SpeedMode mode)
        {
            NativeCalls.SetSpeedMode(_context, mode);
        }

        /// <summary>
        /// Decodes an image with ASTC4x4
        /// </summary>
        /// <param name="from">File to be converted.</param>
        /// <param name="to">File to write the data in.</param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Decode(string from, string to) => Decode(from, to, BlockMode.ASTC4x4);

        /// <summary>
        /// Decodes an image.
        /// </summary>
        /// <param name="from">File to be converted.</param>
        /// <param name="to">File to write the data in.</param>
        /// <param name="blockMode"><see cref="BlockMode"/></param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Decode(string from, string to, BlockMode blockMode)
        {
            SetupConversion(from, to, blockMode);

            NativeCalls.SetMethod(_context, Method.Decompression);

            return (ConvertImageResult)NativeCalls.ConvertImage(_context);
        }

        /// <summary>
        /// Encodes an image with ASTC4x4.
        /// </summary>
        /// <param name="from">File to be converted.</param>
        /// <param name="to">File to write the data in.</param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Encode(string from, string to) => Encode(from, to, BlockMode.ASTC4x4);

        /// <summary>
        /// Encodes an image.
        /// </summary>
        /// <param name="from">File to be converted.</param>
        /// <param name="to">File to write the data in.</param>
        /// <param name="blockMode"><see cref="BlockMode"/></param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Encode(string from, string to, BlockMode blockMode)
        {
            SetupConversion(from, to, blockMode);

            NativeCalls.SetMethod(_context, Method.Compression);

            return (ConvertImageResult)NativeCalls.ConvertImage(_context);
        }

        private void SetupConversion(string from, string to, BlockMode blockMode)
        {
            NativeCalls.SetBlockMode(_context, blockMode);
            NativeCalls.SetInputFile(_context, from);
            NativeCalls.SetOutputFile(_context, to);
        }

        /// <summary>
        /// Set the number of threads to use.
        /// </summary>
        /// <param name="threads"></param>
        public void SetThreadCount(int threads)
        {
            NativeCalls.SetThreadCount(_context, threads);
        }

        ~ASTCContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Destroys the context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {

            }

            NativeCalls.DisposeContext(_context);
            _context = IntPtr.Zero;

            IsDisposed = true;
        }
    }
}
