using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kryptography.Hash
{
    public abstract class BaseHash<T> : IHash
    {
        /// <inheritdoc cref="Compute(string)"/>
        public byte[] Compute(string input)
        {
            return Compute(input, Encoding.ASCII);
        }

        /// <inheritdoc cref="Compute(string,Encoding)"/>
        public byte[] Compute(string input, Encoding enc)
        {
            return Compute(enc.GetBytes(input));
        }

        /// <inheritdoc cref="Compute(Span{byte})"/>
        public byte[] Compute(Span<byte> input)
        {
            return ConvertResult(ComputeValue(input));
        }

        /// <inheritdoc cref="Compute(Stream)"/>
        public byte[] Compute(Stream input)
        {
            return ConvertResult(ComputeValue(input));
        }

        /// <summary>
        /// Computes a hash from a string encoded in ASCII.
        /// </summary>
        /// <param name="input">The string to compute the hash to.</param>
        /// <returns>The computed hash.</returns>
        public T ComputeValue(string input)
        {
            return ComputeValue(input, Encoding.ASCII);
        }

        /// <summary>
        /// Computes a hash from a string.
        /// </summary>
        /// <param name="input">The string to compute the hash to.</param>
        /// <param name="enc">The encoding the string should be encoded in.</param>
        /// <returns>The computed hash.</returns>
        public T ComputeValue(string input, Encoding enc)
        {
            return ComputeValue(enc.GetBytes(input));
        }

        /// <summary>
        /// Computes a hash over a stream of data.
        /// </summary>
        /// <param name="input">The stream of data to hash.</param>
        /// <returns>The computed hash.</returns>
        public T ComputeValue(Span<byte> input)
        {
            var result = CreateInitialValue();
            ComputeInternal(input, ref result);

            FinalizeResult(ref result);
            return result;
        }

        /// <summary>
        /// Computes a hash over an array of bytes.
        /// </summary>
        /// <param name="input">The array of bytes to hash.</param>
        /// <returns>The computed hash.</returns>
        public T ComputeValue(Stream input)
        {
            var result = CreateInitialValue();

            var buffer = new byte[4096];
            int readSize;
            do
            {
                readSize = input.Read(buffer, 0, 4096);
                ComputeInternal(buffer, ref result);
            } while (readSize > 0);

            FinalizeResult(ref result);

            return result;
        }

        /// <summary>
        /// Creates the start value of the hash computation of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The initial value for the hash computation.</returns>
        protected abstract T CreateInitialValue();

        /// <summary>
        /// Applies operations on the computed hash after all input data was consumed.
        /// </summary>
        /// <param name="result">The computed hash after all data was consumed.</param>
        protected abstract void FinalizeResult(ref T result);

        /// <summary>
        /// Computes the hash on a given span of data. This method may be called multiple times and may be handled as an accumulative operation.
        /// </summary>
        /// <param name="input">The data to consume.</param>
        /// <param name="result">The value to hold the computed hash.</param>
        protected abstract void ComputeInternal(Span<byte> input, ref T result);

        /// <summary>
        /// Converts the value of type <typeparamref name="T"/> to a byte array.
        /// </summary>
        /// <param name="result">The finalized computed hash.</param>
        /// <returns>The byte array representing the finalized computed hash.</returns>
        protected abstract byte[] ConvertResult(T result);
    }
}
