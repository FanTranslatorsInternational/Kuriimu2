using System.Buffers.Binary;

namespace plugin_criware.Archives
{
    class RofsCryptoStream : Stream
    {
        private static readonly int[] Primes =
        {
            16411, 16417, 16421, 16427, 16433, 16447, 16451, 16453,
            16477, 16481, 16487, 16493, 16519, 16529, 16547, 16553,
            16561, 16567, 16573, 16603, 16607, 16619, 16631, 16633,
            16649, 16651, 16657, 16661, 16673, 16691, 16693, 16699,
            16703, 16729, 16741, 16747, 16759, 16763, 16787, 16811,
            16823, 16829, 16831, 16843, 16871, 16879, 16883, 16889,
            16901, 16903, 16921, 16927, 16931, 16937, 16943, 16963,
            16979, 16981, 16987, 16993, 17011, 17021, 17027, 17029,
            17033, 17041, 17047, 17053, 17077, 17093, 17099, 17107,
            17117, 17123, 17137, 17159, 17167, 17183, 17189, 17191,
            17203, 17207, 17209, 17231, 17239, 17257, 17291, 17293,
            17299, 17317, 17321, 17327, 17333, 17341, 17351, 17359,
            17377, 17383, 17387, 17389, 17393, 17401, 17417, 17419,
            17431, 17443, 17449, 17467, 17471, 17477, 17483, 17489,
            17491, 17497, 17509, 17519, 17539, 17551, 17569, 17573,
            17579, 17581, 17597, 17599, 17609, 17623, 17627, 17657,
            17659, 17669, 17681, 17683, 17707, 17713, 17729, 17737,
            17747, 17749, 17761, 17783, 17789, 17791, 17807, 17827,
            17837, 17839, 17851, 17863, 17881, 17891, 17903, 17909,
            17911, 17921, 17923, 17929, 17939, 17957, 17959, 17971,
            17977, 17981, 17987, 17989, 18013, 18041, 18043, 18047,
            18049, 18059, 18061, 18077, 18089, 18097, 18119, 18121,
            18127, 18131, 18133, 18143, 18149, 18169, 18181, 18191,
            18199, 18211, 18217, 18223, 18229, 18233, 18251, 18253,
            18257, 18269, 18287, 18289, 18301, 18307, 18311, 18313,
            18329, 18341, 18353, 18367, 18371, 18379, 18397, 18401,
            18413, 18427, 18433, 18439, 18443, 18451, 18457, 18461,
            18481, 18493, 18503, 18517, 18521, 18523, 18539, 18541,
            18553, 18583, 18587, 18593, 18617, 18637, 18661, 18671,
            18679, 18691, 18701, 18713, 18719, 18731, 18743, 18749,
            18757, 18773, 18787, 18793, 18797, 18803, 18839, 18859,
            18869, 18899, 18911, 18913, 18917, 18919, 18947, 18959,
            18973, 18979, 19001, 19009, 19013, 19031, 19037, 19051,
            19069, 19073, 19079, 19081, 19087, 19121, 19139, 19141,
            19157, 19163, 19181, 19183, 19207, 19211, 19213, 19219,
            19231, 19237, 19249, 19259, 19267, 19273, 19289, 19301,
            19309, 19319, 19333, 19373, 19379, 19381, 19387, 19391,
            19403, 19417, 19421, 19423, 19427, 19429, 19433, 19441,
            19447, 19457, 19463, 19469, 19471, 19477, 19483, 19489,
            19501, 19507, 19531, 19541, 19543, 19553, 19559, 19571,
            19577, 19583, 19597, 19603, 19609, 19661, 19681, 19687,
            19697, 19699, 19709, 19717, 19727, 19739, 19751, 19753,
            19759, 19763, 19777, 19793, 19801, 19813, 19819, 19841,
            19843, 19853, 19861, 19867, 19889, 19891, 19913, 19919,
            19927, 19937, 19949, 19961, 19963, 19973, 19979, 19991,
            19993, 19997, 20011, 20021, 20023, 20029, 20047, 20051,
            20063, 20071, 20089, 20101, 20107, 20113, 20117, 20123,
            20129, 20143, 20147, 20149, 20161, 20173, 20177, 20183,
            20201, 20219, 20231, 20233, 20249, 20261, 20269, 20287,
            20297, 20323, 20327, 20333, 20341, 20347, 20353, 20357,
            20359, 20369, 20389, 20393, 20399, 20407, 20411, 20431,
            20441, 20443, 20477, 20479, 20483, 20507, 20509, 20521,
            20533, 20543, 20549, 20551, 20563, 20593, 20599, 20611,
            20627, 20639, 20641, 20663, 20681, 20693, 20707, 20717,
            20719, 20731, 20743, 20747, 20749, 20753, 20759, 20771,
            20773, 20789, 20807, 20809, 20849, 20857, 20873, 20879,
            20887, 20897, 20899, 20903, 20921, 20929, 20939, 20947,
            20959, 20963, 20981, 20983, 21001, 21011, 21013, 21017,
            21019, 21023, 21031, 21059, 21061, 21067, 21089, 21101,
            21107, 21121, 21139, 21143, 21149, 21157, 21163, 21169,
            21179, 21187, 21191, 21193, 21211, 21221, 21227, 21247,
            21269, 21277, 21283, 21313, 21317, 21319, 21323, 21341,
            21347, 21377, 21379, 21383, 21391, 21397, 21401, 21407,
            21419, 21433, 21467, 21481, 21487, 21491, 21493, 21499,
            21503, 21517, 21521, 21523, 21529, 21557, 21559, 21563,
            21569, 21577, 21587, 21589, 21599, 21601, 21611, 21613,
            21617, 21647, 21649, 21661, 21673, 21683, 21701, 21713,
            21727, 21737, 21739, 21751, 21757, 21767, 21773, 21787,
            21799, 21803, 21817, 21821, 21839, 21841, 21851, 21859,
            21863, 21871, 21881, 21893, 21911, 21929, 21937, 21943,
            21961, 21977, 21991, 21997, 22003, 22013, 22027, 22031,
            22037, 22039, 22051, 22063, 22067, 22073, 22079, 22091,
            22093, 22109, 22111, 22123, 22129, 22133, 22147, 22153,
            22157, 22159, 22171, 22189, 22193, 22229, 22247, 22259,
            22271, 22273, 22277, 22279, 22283, 22291, 22303, 22307,
            22343, 22349, 22367, 22369, 22381, 22391, 22397, 22409,
            22433, 22441, 22447, 22453, 22469, 22481, 22483, 22501,
            22511, 22531, 22541, 22543, 22549, 22567, 22571, 22573,
            22613, 22619, 22621, 22637, 22639, 22643, 22651, 22669,
            22679, 22691, 22697, 22699, 22709, 22717, 22721, 22727,
            22739, 22741, 22751, 22769, 22777, 22783, 22787, 22807,
            22811, 22817, 22853, 22859, 22861, 22871, 22877, 22901,
            22907, 22921, 22937, 22943, 22961, 22963, 22973, 22993,
            23003, 23011, 23017, 23021, 23027, 23029, 23039, 23041,
            23053, 23057, 23059, 23063, 23071, 23081, 23087, 23099,
            23117, 23131, 23143, 23159, 23167, 23173, 23189, 23197,
            23201, 23203, 23209, 23227, 23251, 23269, 23279, 23291,
            23293, 23297, 23311, 23321, 23327, 23333, 23339, 23357,
            23369, 23371, 23399, 23417, 23431, 23447, 23459, 23473,
            23497, 23509, 23531, 23537, 23539, 23549, 23557, 23561,
            23563, 23567, 23581, 23593, 23599, 23603, 23609, 23623,
            23627, 23629, 23633, 23663, 23669, 23671, 23677, 23687,
            23689, 23719, 23741, 23743, 23747, 23753, 23761, 23767,
            23773, 23789, 23801, 23813, 23819, 23827, 23831, 23833,
            23857, 23869, 23873, 23879, 23887, 23893, 23899, 23909,
            23911, 23917, 23929, 23957, 23971, 23977, 23981, 23993,
            24001, 24007, 24019, 24023, 24029, 24043, 24049, 24061,
            24071, 24077, 24083, 24091, 24097, 24103, 24107, 24109,
            24113, 24121, 24133, 24137, 24151, 24169, 24179, 24181,
            24197, 24203, 24223, 24229, 24239, 24247, 24251, 24281,
            24317, 24329, 24337, 24359, 24371, 24373, 24379, 24391,
            24407, 24413, 24419, 24421, 24439, 24443, 24469, 24473,
            24481, 24499, 24509, 24517, 24527, 24533, 24547, 24551,
            24571, 24593, 24611, 24623, 24631, 24659, 24671, 24677,
            24683, 24691, 24697, 24709, 24733, 24749, 24763, 24767,
            24781, 24793, 24799, 24809, 24821, 24841, 24847, 24851,
            24859, 24877, 24889, 24907, 24917, 24919, 24923, 24943,
            24953, 24967, 24971, 24977, 24979, 24989, 25013, 25031,
            25033, 25037, 25057, 25073, 25087, 25097, 25111, 25117,
            25121, 25127, 25147, 25153, 25163, 25169, 25171, 25183,
            25189, 25219, 25229, 25237, 25243, 25247, 25253, 25261,
            25301, 25303, 25307, 25309, 25321, 25339, 25343, 25349,
            25357, 25367, 25373, 25391, 25409, 25411, 25423, 25439,
            25447, 25453, 25457, 25463, 25469, 25471, 25523, 25537,
            25541, 25561, 25577, 25579, 25583, 25589, 25601, 25603,
            25609, 25621, 25633, 25639, 25643, 25657, 25667, 25673,
            25679, 25693, 25703, 25717, 25733, 25741, 25747, 25759,
            25763, 25771, 25793, 25799, 25801, 25819, 25841, 25847,
            25849, 25867, 25873, 25889, 25903, 25913, 25919, 25931,
            25933, 25939, 25943, 25951, 25969, 25981, 25997, 25999,
            26003, 26017, 26021, 26029, 26041, 26053, 26083, 26099,
            26107, 26111, 26113, 26119, 26141, 26153, 26161, 26171,
            26177, 26183, 26189, 26203, 26209, 26227, 26237, 26249,
            26251, 26261, 26263, 26267, 26293, 26297, 26309, 26317,
            26321, 26339, 26347, 26357, 26371, 26387, 26393, 26399,
            26407, 26417, 26423, 26431, 26437, 26449, 26459, 26479,
            26489, 26497, 26501, 26513, 26539, 26557, 26561, 26573,
            26591, 26597, 26627, 26633, 26641, 26647, 26669, 26681
        };

        private static readonly string[] Scrambles = { 
          // t 0  1  2  3  4  5  6  7
            "^03 .0 37 .4 .1 26 .2 15",  //0
            "^12 .7 .5 23 00 .6 .4 31",  //1
            "^.1 27 .6 12 35 .3 00 .4",  //2
            "+23 .6 .0 .2 04 11 .7 35",  //3
            "+.7 30 02 16 .4 .3 .5 21",  //4
            "+.2 23 .6 07 .0 11 .4 35",  //5
            "+03 .7^12 .6 .1 25 .0+34",  //6
            " .7^34 .3+21 .0 .2 15^06",  //7
            " .3^10 .6+04^32 .7 .1+25",  //8
        };

        private readonly Stream _baseStream;
        private readonly byte[] _initialKey;
        private readonly byte[] _localKey;
        private readonly byte[] _sector;
        private readonly int _startSector;
        private readonly int _sectorSize;

        private long _position;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public RofsCryptoStream(Stream baseStream, string password, int startSector, int sectorSize)
        {
            _baseStream = baseStream;
            _initialKey = InitializeKey(password);
            _localKey = new byte[_initialKey.Length];
            _sector = new byte[sectorSize];
            _startSector = startSector;
            _sectorSize = sectorSize;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;

                case SeekOrigin.Current:
                    _position += offset;
                    break;

                case SeekOrigin.End:
                    _position = Length + offset;
                    break;
            }

            return _position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var startPos = _baseStream.Position;

            var result = count = (int)Math.Min(Length - _position, count);

            // Determine starting sector information
            var posSector = (int)(_position / _sectorSize);
            var sectorCount = (_position + count) / _sectorSize - posSector + ((_position + count) % _sectorSize > 0 ? 1 : 0);

            // Read and decrypt sectors
            _baseStream.Position = posSector * _sectorSize;

            var firstSector = posSector + _startSector;
            for (var i = 0; i < sectorCount; i++)
            {
                // Read
                var sectorLength = (int)Math.Min(_sectorSize, Length - _position / _sectorSize * _sectorSize);
                _baseStream.Read(_sector, 0, sectorLength);

                if (sectorLength != _sectorSize)
                    Array.Clear(_sector, sectorLength, _sectorSize - sectorLength);

                // Decrypt
                Process(_sector, firstSector + i);

                // Copy data to output
                var length = (int)Math.Min(count, sectorLength - _position % _sectorSize);
                Array.Copy(_sector, _position % _sectorSize, buffer, offset, length);

                // Advance variables
                offset += length;
                _position += length;
                count -= length;
            }

            _baseStream.Position = startPos;

            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var startPos = _baseStream.Position;
            var result = count;

            // Determine starting sector information
            var posSector = (int)(_position / _sectorSize);
            var sectorCount = (_position + count) / _sectorSize - posSector + ((_position + count) % _sectorSize > 0 ? 1 : 0);

            // Read, encrypt, write sectors
            _baseStream.Position = posSector * _sectorSize;

            var firstSector = posSector + _startSector;
            for (var i = 0; i < sectorCount; i++)
            {
                // Read
                var sectorLength = (int)Math.Min(_sectorSize, Length - _position / _sectorSize * _sectorSize);
                _baseStream.Read(_sector, 0, sectorLength);

                if (sectorLength != _sectorSize)
                    Array.Clear(_sector, sectorLength, _sectorSize - sectorLength);

                // Decrypt
                Process(_sector, firstSector + i);

                // Fill sector
                var length = (int)Math.Min(count, _sectorSize - _position % _sectorSize);
                Array.Copy(buffer, offset, _sector, _position % _sectorSize, length);

                // Encrypt
                Process(_sector, firstSector + i);

                // Write
                _baseStream.Position = _position / _sectorSize * _sectorSize;
                _baseStream.Write(_sector);

                // Advance variables
                offset += length;
                _position += length;
                count -= length;
            }

            _baseStream.Position = startPos;
        }

        #region Key initialization

        private byte[] InitializeKey(string password)
        {
            // Aggregate password in an integer
            var sum = 0;
            for (var k = 0; k < password.Length; k++)
            {
                sum = password[k] * (password[k] + sum);
                for (var i = k + 1; i < password.Length; i++)
                    sum += password[i];
            }

            // Expand key
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)(sum * 0x100001));

            var key = new byte[8];
            for (var i = 0; i < 4; i++)
            {
                key[i * 2] = buffer[i];
                key[i * 2 + 1] = buffer[3 - i];
            }

            // Hash key
            ApplyExtraHash(key);

            return key;
        }

        private void ApplyExtraHash(byte[] key)
        {
            for (var i = 0; i < 8; i += 2)
            {
                var part = key.AsSpan(i, 2);
                BinaryPrimitives.WriteUInt16BigEndian(part, CreateHashValue(part, 18973));
            }
        }

        private ushort CreateHashValue(Span<byte> span, ushort initialValue)
        {
            var result = initialValue;
            foreach (var b in span)
            {
                var p = Primes[128 + (sbyte)b] * result;
                result = (ushort)Primes[p & 0x3FF];
            }

            return result;
        }

        #endregion

        #region Sector cipher

        private void Process(Span<byte> sector, int sectorIndex)
        {
            var seed = (int)_initialKey[5];
            for (var i = 0; i < sector.Length; i += _initialKey.Length)
            {
                // Get local key
                var hash = CalculateKeyHash(seed * sectorIndex, out var scrambleIndex);
                CalculateLocalKey(hash, scrambleIndex);

                // Process with local key
                seed = scrambleIndex + i;
                for (var k = 0; k < _initialKey.Length; k++)
                {
                    sector[i + k] ^= _localKey[k];
                    seed *= _localKey[k];
                }
            }
        }

        private byte[] CalculateKeyHash(int seed, out int scrambleIndex)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)(seed * 0x100001));

            scrambleIndex = CreateHashValue(buffer, 18973) % 9;

            var hash2 = CreateHashValue(buffer, 21503);
            var hash3 = CreateHashValue(buffer, 24001);
            BinaryPrimitives.WriteUInt16BigEndian(buffer, hash2);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), hash3);

            return buffer;
        }

        private void CalculateLocalKey(byte[] hash, int scrambleIndex)
        {
            var scramble = Scrambles[scrambleIndex];
            var s = 0;

            var type = '^';
            for (var i = 0; i < 8; i++)
            {
                while (scramble[s] == ' ') s++;
                if (scramble[s] == '^' || scramble[s] == '+') type = scramble[s++];

                var o1 = scramble[s++];
                var o2 = scramble[s++];
                var b = _initialKey[o2 - '0'];

                if (o1 != '.')
                {
                    if (type == '^') b ^= hash[o1 - '0'];
                    else b += hash[o1 - '0'];
                }

                _localKey[i] = b;
            }
        }

        #endregion
    }
}
