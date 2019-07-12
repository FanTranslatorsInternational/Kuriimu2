//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Kompression.LempelZiv.MatchFinder.Models
//{
//    class SuffixArrayElegant
//    {
//        public static int[] Build(byte[] input)
//        {
//            Initialize(out var maskRight, out var maskLeft, out var mask);

//            SuffixArrayUtils.RenameAlphabet(input, input.Length, out var charCode, out var bitsPerChar);

//            var bitsPerFirstPass = 16 / bitsPerChar * bitsPerChar;
//            var suffixArray = new uint[input.Length];

//            var minBitsSortedInSecondPass = InputBasedSort(bitsPerFirstPass, input, input.Length, bitsPerChar, charCode, maskRight, suffixArray);
//            expectedSorted = (bitsPerFirstPass + minBitsSortedInSecondPass)
//                             / bitsPerChar;

//            //		printTime("First two passes");
//            //		printBucketStats();

//            uint sentinels = 100; // add sentinels to avoid if's in getBucketWord
//            bucketVec.reserve(length + sentinels);
//            bucket = &bucketVec[0];
//            memset(bucket + length, 0, sentinels * sizeof(uint));

//            finalTouches();

//            return suffixArray;
//        }

//        private static void Initialize(out uint[] maskRight, out uint[] maskLeft, out uint[][] mask)
//        {
//            maskRight = new uint[32];
//            maskLeft = new uint[32];
//            mask = new uint[32][];
//            for (var i = 0; i < 32; i++)
//                mask[i] = new uint[32];

//            for (var i = 0; i < 32; i++)
//            {
//                var ones = (1u << i) - 1;
//                maskRight[i] = ones;
//                maskLeft[i] = ones << (32 - i);
//                for (var j = 0; j < 32; j++, ones <<= 1)
//                    mask[i][j] = ones;
//            }
//        }

//        private static int InputBasedSort(int bitsPerFirstPass, byte[] input, int length, int bitsPerChar, uint[] charCode, uint[] maskRight, uint[] suffixArray)
//        {
//            var vectors = new List<uint>(2 + length * bitsPerChar / 32);
//            PrepareInput(vectors, input, length, bitsPerChar, charCode, maskRight);

//            var n = 1 << bitsPerFirstPass;
//            var bSizeVec = new List<uint>(n);
//            var bStartVec = new List<uint>(n);
//            FirstPass(bitsPerFirstPass, bitsPerChar, charCode, bStartVec, bSizeVec, input, suffixArray);

//            var bits = SecondPass(bitsPerFirstPass, bStartVec, bSizeVec);
//            uint bits =SecondPass(bitsPerFirstPass, n, &bStartVec[0], &bSizeVec[0]);
//            return bits;
//        }

//        private static void PrepareInput(List<uint> vectors, byte[] input, int length, int bitsPerChar, uint[] charCode, uint[] maskRight)
//        {
//            var remainBits = 32;
//            uint l = 0;
//            var nWords = 0;
//            for (uint i = 0; i < length; ++i)
//            {
//                var code = charCode[input[i]];
//                remainBits -= bitsPerChar;
//                if (remainBits >= 0)
//                {
//                    l = (l << bitsPerChar) | code;
//                }
//                else
//                {
//                    var bitsInNextWord = -remainBits;
//                    var bitsAtEnd = bitsPerChar - bitsInNextWord;
//                    l = (l << bitsAtEnd) | (code >> bitsInNextWord);
//                    vectors[nWords++] = l;
//                    l = code & maskRight[bitsInNextWord];
//                    remainBits = 32 - bitsInNextWord;
//                }
//            }
//            if (remainBits < 32)
//                vectors[nWords++] = l << remainBits;
//            vectors[nWords] = 0;
//        }

//        private static void FirstPass(int bitsPerFirstPass, int bitsPerChar, uint[] charCode, List<uint> bStartVec, List<uint> bSizeVec, byte[] input, uint[] suffixArray)
//        {
//            ShiftCode(bitsPerFirstPass - bitsPerChar, charCode, out var shiftedCode);

//            var w = 0u;
//            for (var i = input.Length - 1; i >= 0; i--)
//            {
//                w >>= bitsPerChar;
//                w |= shiftedCode[input[i]];
//                bSizeVec[(int)w]++;
//            }

//            bStartVec[0] = 0;
//            SuffixArrayUtils.PrefixSum(new Span<uint>(bSizeVec.ToArray()), new Span<uint>(bStartVec.ToArray(), 1, bStartVec.Capacity - 1), bSizeVec.Capacity - 1);

//            w = 0;
//            for (var i = input.Length - 1; i >= 0; i--)
//            {
//                w >>= bitsPerChar;
//                w |= shiftedCode[input[i]];
//                suffixArray[bStartVec[(int)w]++] = (uint)i;
//            }

//            for (int i = 0; i < bStartVec.Capacity; ++i)
//                bStartVec[i] -= bSizeVec[i];

//            var bitVectorSize = input.Length + 1;
//            CreateBucketFlags(bitVectorSize, out var bucketFlagVec);
//            bucketFlagVec[input.Length] = 1;

//            var zeros = FixEndingZeros(input, charCode, bucketFlagVec);
//            bStartVec[0] += zeros;
//            bSizeVec[0] -= zeros;
//        }

//        private static void ShiftCode(int bitsAtATime, uint[] charCode, out uint[] shiftedCode)
//        {
//            shiftedCode = new uint[256];
//            for (var i = 0; i < 256; ++i)
//                shiftedCode[i] = charCode[i] << bitsAtATime;
//        }

//        private static void CreateBucketFlags(int bitVectorSize, out List<byte> bucketFlagVec)
//        {
//            bucketFlagVec = new List<byte>(bitVectorSize);
//            //bucketFlag = &bucketFlagVec[0];
//            //memset(bucketFlag, 0, bitVectorSize * sizeof(uchar));
//        }

//        private static uint FixEndingZeros(byte[] input, uint[] charCode, List<byte> bucketFlagVec)
//        {
//            var p = 0;
//            for (var i = input.Length - 1; i != 0 && charCode[input[i]] == 0; i--)
//                bucketFlagVec[p++] = 1;
//            return (uint)p;
//        }

//        private static uint SecondPass(int bitsToSkip,List<uint> bStart,List<uint> bSize)
//        {
//            RadixLSDCache<word> sorter;
//            uint maxSize = max(bSize, nBuckets);
//            vector<word> bufferVec(maxSize);
//            word* buffer = &bufferVec[0];
//            for (uint i = 0; i < nBuckets; ++i)
//            {
//                uint start = bStart[i];
//                bucketFlag[start] = 1;

//                uint bLen = bSize[i];
//                if (bLen > 1)
//                {
//                    uint* lo = sa + start;
//                    copyWords(lo, bLen, buffer, bitsToSkip);
//                    sorter.sort(bLen, lo, buffer);
//                    updateBucketStart(buffer, bLen, start);
//                }
//            }

//            return bitsPerWord;
//        }
//    }

//    class SuffixArrayUtils
//    {
//        public static int RenameAlphabet(byte[] input, int position, out uint[] code, out int codeLength)
//        {
//            code = new uint[256];

//            for (var i = position; i < input.Length - position; i++)
//                code[input[i]] = 1;

//            code[0] -= 1;
//            PrefixSum(code, code, 256);

//            var alphabetSize = (int)code[255] + 1;
//            codeLength = BitsFor(alphabetSize - 1);

//            return alphabetSize;
//        }

//        public static void PrefixSum(Span<uint> input, Span<uint> output, int length)
//        {
//            output[0] = input[0];
//            for (var i = 1; i < length; i++)
//                output[i] = input[i] + output[i - 1];
//        }

//        private static int BitsFor(int n)
//        {
//            var b = 1;
//            if (n >= 0x10000)
//            {
//                b += 16;
//                n >>= 16;
//            }
//            if (n >= 0x100)
//            {
//                b += 8;
//                n >>= 8;
//            }
//            if (n >= 0x10)
//            {
//                b += 4;
//                n >>= 4;
//            }
//            if (n >= 4)
//            {
//                b += 2;
//                n >>= 2;
//            }
//            if (n >= 2)
//                b += 1;

//            return b;
//        }
//    }
//}
