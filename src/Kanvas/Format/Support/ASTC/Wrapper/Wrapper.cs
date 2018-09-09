using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Support.ASTC
{
    public enum ConvertImageResult : int
    {
        OK = 0,
        Error = 1
    }

    public enum BlockMode : int
    {
        ASTC4x4 = 0,
        ASTC5x4 = 1,
        ASTC5x5 = 2,
        ASTC6x5 = 3,
        ASTC6x6 = 4,
        ASTC8x5 = 5,
        ASTC8x6 = 6,
        ASTC8x8 = 7,
        ASTC10x5 = 8,
        ASTC10x6 = 9,
        ASTC10x8 = 10,
        ASTC10x10 = 11,
        ASTC12x10 = 12,
        ASTC12x12 = 13,

        ASTC3x3x3 = 14,
        ASTC4x3x3 = 15,
        ASTC4x4x3 = 16,
        ASTC4x4x4 = 17,
        ASTC5x4x4 = 18,
        ASTC5x5x4 = 19,
        ASTC5x5x5 = 20,
        ASTC6x5x5 = 21,
        ASTC6x6x5 = 22,
        ASTC6x6x6 = 23,
    }

    public enum DecodeMode : int
    {
        LDR_SRGB = 0,
        LDR = 1,
        //HDR = 2       //HDR will be disabled for now, since it sets KTX to bitness 16, which triggers HalfFloat usage
        //Until HalfFloat can be read, it will be left disabled
    }

    public enum SpeedMode : int
    {
        Veryfast = 0,
        Fast = 1,
        Medium = 2,
        Thorough = 3,
        Exhaustive = 4
    }

    /// <summary>
    /// Creates a new ASTC context
    /// </summary>
    public class ASTCContext : IDisposable
    {
        IntPtr _context;

        /// <summary>
        /// Creates a new ASTC context
        /// </summary>
        public ASTCContext()
        {
            _context = NativeCalls.CreateContext();
        }

        /// <summary>
        /// Sets the mode to decode the image with
        /// </summary>
        /// <param name="mode"><see cref="DecodeMode"/></param>
        public void SetDecodeMode(DecodeMode mode)
        {
            NativeCalls.SetDecodeMode(_context, mode);
        }

        /// <summary>
        /// Sets the speed and therefore accuracy of the conversion
        /// </summary>
        /// <param name="mode"><see cref="SpeedMode"/></param>
        public void SetSpeedMode(SpeedMode mode)
        {
            NativeCalls.SetSpeedMode(_context, mode);
        }

        /// <summary>
        /// Decodes an image with ASTC4x4
        /// </summary>
        /// <param name="from">File to be converted</param>
        /// <param name="to">File to write the data in</param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Decode(string from, string to) => Decode(from, to, BlockMode.ASTC4x4);

        /// <summary>
        /// Decodes an image
        /// </summary>
        /// <param name="from">File to be converted</param>
        /// <param name="to">File to write the data in</param>
        /// <param name="blockMode"><see cref="BlockMode"/></param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Decode(string from, string to, BlockMode blockMode)
        {
            SetupConversion(from, to, blockMode);

            NativeCalls.SetMethod(_context, NativeCalls.Method.Decompression);

            return (ConvertImageResult)NativeCalls.ConvertImage(_context);
        }

        /// <summary>
        /// Encodes an image
        /// </summary>
        /// <param name="from">File to be converted</param>
        /// <param name="to">File to write the data in</param>
        /// <param name="blockMode"><see cref="BlockMode"/></param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Encode(string from, string to) => Encode(from, to, BlockMode.ASTC4x4);

        /// <summary>
        /// Encodes an image
        /// </summary>
        /// <param name="from">File to be converted</param>
        /// <param name="to">File to write the data in</param>
        /// <param name="blockMode"><see cref="BlockMode"/></param>
        /// <returns><see cref="ConvertImageResult"/></returns>
        public ConvertImageResult Encode(string from, string to, BlockMode blockMode)
        {
            SetupConversion(from, to, blockMode);

            NativeCalls.SetMethod(_context, NativeCalls.Method.Compression);

            return (ConvertImageResult)NativeCalls.ConvertImage(_context);
        }

        private void SetupConversion(string from, string to, BlockMode blockMode)
        {
            NativeCalls.SetBlockMode(_context, blockMode);
            NativeCalls.SetInputFile(_context, from);
            NativeCalls.SetOutputFile(_context, to);
        }

        /// <summary>
        /// Set the number of threads to use
        /// </summary>
        /// <param name="threads"></param>
        public void SetThreadCount(int threads)
        {
            NativeCalls.SetThreadCount(_context, threads);
        }

        /// <summary>
        /// Destroys the context
        /// </summary>
        public void Dispose()
        {
            NativeCalls.DisposeContext(_context);
            _context = IntPtr.Zero;
        }
    }
}
