using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace XamlAnimatedGif.Decompression
{
    public static class Lzw
    {
        private const int MaxCodeLength = 12;

        public static void Decompress(ReadOnlySpan<byte> source, Stream destination, int minimumCodeLength)
        {
            var bitReader = new BitReader(source);
            var codeTable = new CodeTable(minimumCodeLength);
            int prevCode = 0;

            int code;
            do
            {
                code = bitReader.ReadBits(codeTable.CodeLength);
            } while (ProcessCode());

            void ResetCodeTable()
            {
                codeTable.Reset();
                prevCode = -1;
            }
            
            bool ProcessCode()
            {
                if (code < codeTable.Count)
                {
                    var sequence = codeTable[code];
                    if (sequence.IsStopCode)
                    {
                        return false;
                    }
                    if (sequence.IsClearCode)
                    {
                        ResetCodeTable();
                        return true;
                    }

                    sequence.CopyTo(destination);

                    if (prevCode >= 0)
                    {
                        var prev = codeTable[prevCode];
                        var newSequence = prev.Append(sequence.Bytes[0]);
                        codeTable.Add(newSequence);
                    }
                }
                else
                {
                    var prev = codeTable[prevCode];
                    var newSequence = prev.Append(prev.Bytes[0]);
                    codeTable.Add(newSequence);

                    newSequence.CopyTo(destination);
                }
                prevCode = code;
                return true;
            }
        }
        
        private readonly struct Sequence
        {
            public Sequence(byte[] bytes)
                : this()
            {
                Bytes = bytes;
            }

            private Sequence(bool isClearCode, bool isStopCode)
                : this()
            {
                IsClearCode = isClearCode;
                IsStopCode = isStopCode;
            }

            public byte[] Bytes { get; }

            public bool IsClearCode { get; }

            public bool IsStopCode { get; }

            public static Sequence ClearCode { get; } = new Sequence(true, false);

            public static Sequence StopCode { get; } = new Sequence(false, true);

            public Sequence Append(byte b)
            {
                var bytes = new byte[Bytes.Length + 1];
                Bytes.CopyTo(bytes, 0);
                bytes[Bytes.Length] = b;
                return new Sequence(bytes);
            }

            public void CopyTo(Stream stream)
            {
                stream.Write(Bytes, 0, Bytes.Length);
            }
        }

        class CodeTable
        {
            private readonly int _minimumCodeLength;
            private readonly Sequence[] _table;
            private int _count;
            private int _codeLength;

            public CodeTable(int minimumCodeLength)
            {
                _minimumCodeLength = minimumCodeLength;
                _codeLength = _minimumCodeLength + 1;
                int initialEntries = 1 << minimumCodeLength;
                _table = new Sequence[1 << MaxCodeLength];
                for (int i = 0; i < initialEntries; i++)
                {
                    _table[_count++] = new Sequence(new[] {(byte) i});
                }
                Add(Sequence.ClearCode);
                Add(Sequence.StopCode);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _count = (1 << _minimumCodeLength) + 2;
                _codeLength = _minimumCodeLength + 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Sequence sequence)
            {
                // Code table is full, stop adding new codes
                if (_count >= _table.Length)
                    return;

                _table[_count++] = sequence;
                if ((_count & (_count - 1)) == 0 && _codeLength < MaxCodeLength)
                    _codeLength++;
            }

            public Sequence this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _table[index];
                }
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _count;
            }

            public int CodeLength
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _codeLength;
            }
        }
        
        ref struct BitReader
        {
            private readonly ReadOnlySpan<byte> _buffer;

            public BitReader(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
                _bytePosition = -1;
                _bitPosition = 0;
                _currentValue = -1;
            }

            private int _bytePosition;
            private int _bitPosition;
            private int _currentValue;

            public int ReadBits(int bitCount)
            {
                // The following code assumes it's running on a little-endian architecture.
                // It's probably safe to assume it will always be the case, because:
                // - Windows only supports little-endian architectures: x86/x64 and ARM (which supports
                //   both endiannesses, but Windows on ARM is always in little-endian mode)
                // - No platforms other than Windows support XAML applications
                // If the situation changes, this code will have to be updated.

                if (_bytePosition == -1)
                {
                    _bytePosition = 0;
                    _bitPosition = 0;
                    _currentValue = ReadInt32();
                }
                else if (bitCount > 32 - _bitPosition)
                {
                    int n = _bitPosition >> 3;
                    _bytePosition += n;
                    _bitPosition &= 0x07;
                    _currentValue = ReadInt32() >> _bitPosition;
                }
                int mask = (1 << bitCount) - 1;
                int value = _currentValue & mask;
                _currentValue >>= bitCount;
                _bitPosition += bitCount;
                return value;
            }

            private int ReadInt32()
            {
                int value = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (_bytePosition + i >= _buffer.Length)
                        break;
                    value |= _buffer[_bytePosition + i] << (i << 3);
                }
                return value;
            }
        }
    }
}