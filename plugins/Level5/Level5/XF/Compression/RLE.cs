using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using System;

namespace Kore.XFont.Compression
{
    public class RLE
    {
        public static byte[] Decompress(Stream instream, long decompressedLength)
        {
            using (var br = new BinaryReaderX(instream, true))
            {
                var result = new List<byte>();

                while (true)
                {
                    var flag = br.ReadByte();
                    result.AddRange(flag >= 128
                        ? Enumerable.Repeat(br.ReadByte(), flag - 128 + 3)
                        : br.ReadBytes(flag + 1));

                    if (result.Count == decompressedLength)
                    {
                        return result.ToArray();
                    }
                    else if (result.Count > decompressedLength)
                    {
                        throw new InvalidDataException("Went past the end of the stream");
                    }
                }
            }
        }

        //Code from 3DS Explorer source
        //https://github.com/svn2github/3DS-Explorer/blob/master/3DSExplorer/DSDecmp/Formats/Nitro/RLE.cs
        public static byte[] Compress(Stream instream)
        {
            long inLength = instream.Length;

            var outstream = new MemoryStream();

            if (inLength > 0xFFFFFF)
                throw new System.Exception("Stream too large");

            List<byte> compressedData = new List<byte>();

            // at most 0x7F+3=130 bytes are compressed into a single block.
            // (and at most 0x7F+1=128 in an uncompressed block, however we need to read 2
            // more, since the last byte may be part of a repetition).
            byte[] dataBlock = new byte[130];
            // the length of the valid content in the current data block
            int currentBlockLength = 0;

            int readLength = 0;
            int nextByte;
            int repCount;
            while (readLength < inLength)
            {
                bool foundRepetition = false;
                repCount = 1;

                while (currentBlockLength < dataBlock.Length && readLength < inLength)
                {
                    nextByte = instream.ReadByte();
                    if (nextByte < 0)
                        throw new System.Exception("Stream too short");
                    readLength++;

                    dataBlock[currentBlockLength++] = (byte)nextByte;
                    if (currentBlockLength > 1)
                    {
                        if (nextByte == dataBlock[currentBlockLength - 2])
                            repCount++;
                        else
                            repCount = 1;
                    }

                    foundRepetition = repCount > 2;
                    if (foundRepetition)
                        break;
                }


                int numUncompToCopy = 0;
                if (foundRepetition)
                {
                    // if a repetition was found, copy block size - 3 bytes as compressed data
                    numUncompToCopy = currentBlockLength - 3;
                }
                else
                {
                    // if no repetition was found, copy min(block size, max block size - 2) bytes as uncompressed data.
                    numUncompToCopy = Math.Min(currentBlockLength, dataBlock.Length - 2);
                }

                #region insert uncompressed block
                if (numUncompToCopy > 0)
                {
                    byte flag = (byte)(numUncompToCopy - 1);
                    compressedData.Add(flag);
                    for (int i = 0; i < numUncompToCopy; i++)
                        compressedData.Add(dataBlock[i]);
                    // shift some possibly remaining bytes to the start
                    for (int i = numUncompToCopy; i < currentBlockLength; i++)
                        dataBlock[i - numUncompToCopy] = dataBlock[i];
                    currentBlockLength -= numUncompToCopy;
                }
                #endregion

                if (foundRepetition)
                {
                    // if a repetition was found, continue until the first different byte
                    // (or until the buffer is full)
                    while (currentBlockLength < dataBlock.Length && readLength < inLength)
                    {
                        nextByte = instream.ReadByte();
                        if (nextByte < 0)
                            throw new System.Exception("Stream too short");
                        readLength++;

                        dataBlock[currentBlockLength++] = (byte)nextByte;

                        if (nextByte != dataBlock[0])
                            break;
                        else
                            repCount++;
                    }

                    // the next repCount bytes are the same.
                    #region insert compressed block
                    byte flag = (byte)(0x80 | (repCount - 3));
                    compressedData.Add(flag);
                    compressedData.Add(dataBlock[0]);
                    // make sure to shift the possible extra byte to the start
                    if (repCount != currentBlockLength)
                        dataBlock[0] = dataBlock[currentBlockLength - 1];
                    currentBlockLength -= repCount;
                    #endregion
                }
            }

            // write any reamaining bytes as uncompressed
            if (currentBlockLength > 0)
            {
                byte flag = (byte)(currentBlockLength - 1);
                compressedData.Add(flag);
                for (int i = 0; i < currentBlockLength; i++)
                    compressedData.Add(dataBlock[i]);
                currentBlockLength = 0;
            }

            int compLen = compressedData.Count;

            // write the compressed data
            outstream.Write(compressedData.ToArray(), 0, compLen);

            // the total compressed stream length is the compressed data length + the 4-byte header
            outstream.Position = 0;
            return new BinaryReaderX(outstream).ReadBytes((int)outstream.Length);
        }
    }
}
