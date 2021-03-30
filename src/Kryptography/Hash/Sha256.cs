using System;
using System.IO;
using System.Security.Cryptography;

namespace Kryptography.Hash
{
    public class Sha256 : IHash
    {
        private static readonly SHA256 HashInstance = SHA256.Create();

        public byte[] Compute(Span<byte> input) => HashInstance.ComputeHash(input.ToArray());

        public byte[] Compute(Stream input) => HashInstance.ComputeHash(input);
    }
}
