using System;
using System.Buffers;
using System.IO;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif.Decompression
{
    internal class LzwDecompressor
    {
        private const int MaxCodeLength = 12;

        public static void Decompress(Memory<byte> codeStream, int minimumCodeLength, Stream output)
        {
            using var codeTable = new CodeTable(minimumCodeLength);
            var bitReader = new BitReader(codeStream);
            int prevCode = -1;
            while (bitReader.TryReadBits(codeTable.CodeLength, out int code))
            {
                if (code < codeTable.Count)
                {
                    if (codeTable.IsStopCode(code))
                    {
                        return;
                    }

                    if (codeTable.IsClearCode(code))
                    {
                        codeTable.Clear();
                        prevCode = -1;
                        continue;
                    }

                    var sequence = codeTable[code];
                    output.Write(sequence);

                    if (prevCode >= 0)
                    {
                        var prev = codeTable[prevCode];
                        codeTable.AddNextCode(prevCode, sequence[0]);
                    }
                }
                else
                {
                    var prevSequence = codeTable[prevCode];
                    codeTable.AddNextCode(prevCode, prevSequence[0]);

                    output.Write(prevSequence);
                    output.WriteByte(prevSequence[0]);
                }
                prevCode = code;
            }
        }

        private class CodeTable : IDisposable
        {
            // Initial codes, with sequences of length 1, where the code is equal to the color index (0-255)
            private static readonly byte[] InitialCodeTableBuffer;
            private static readonly (int, int)[] InitialCodeTable;

            static CodeTable()
            {
                InitialCodeTableBuffer = new byte[256];
                InitialCodeTable = new (int, int)[256];
                for (int i = 0; i < 256; i++)
                {
                    InitialCodeTableBuffer[i] = (byte)i;
                    InitialCodeTable[i] = (i, 1);
                }
            }

            private readonly int _minimumCodeLength;
            private byte[] _buffer;
            private readonly (int offset, int length)[] _codeTable;
            private readonly int _clearCode;
            private readonly int _stopCode;

            private int _codeLength;
            private int _count;
            private int _nextOffset;

            public CodeTable(int minimumCodeLength)
            {
                _minimumCodeLength = minimumCodeLength;
                var initialEntriesCount = 1 << _minimumCodeLength;
                _clearCode = initialEntriesCount;
                _stopCode = initialEntriesCount + 1;

                _buffer = ArrayPool<byte>.Shared.Rent(1 << MaxCodeLength);
                _codeTable = ArrayPool<(int, int)>.Shared.Rent(1 << MaxCodeLength);
                Buffer.BlockCopy(InitialCodeTableBuffer, 0, _buffer, 0, initialEntriesCount);
                Array.Copy(InitialCodeTable, 0, _codeTable, 0, initialEntriesCount);

                _codeLength = _minimumCodeLength + 1;
                _count = initialEntriesCount + 2;
                _nextOffset = initialEntriesCount + 2;
            }

            public Span<byte> this[int code]
            {
                get
                {
                    var (offset, length) = _codeTable[code];
                    return _buffer.AsSpan(offset, length);
                }
            }

            public bool IsClearCode(int code) => code == _clearCode;
            public bool IsStopCode(int code) => code == _stopCode;

            public int CodeLength => _codeLength;

            public int Count => _count;

            public void Clear()
            {
                _codeLength = _minimumCodeLength + 1;
                var initialEntriesCount = 1 << _minimumCodeLength;
                _count = initialEntriesCount + 2;
                _nextOffset = initialEntriesCount + 2;
            }

            public void AddNextCode(int previousCode, byte newValue)
            {
                var (previousOffset, previousLength) = _codeTable[previousCode];
                int destLength = previousLength + 1;
                if (_count >= _codeTable.Length)
                {
                    // Can't add more codes
                    return;
                }

                int destOffset;
                if (previousCode == _count - 1)
                {
                    // The previous entry is at the end of the table, so just reuse the same bytes
                    // and append the new one. No need to copy anything.
                    destOffset = previousOffset;
                    EnsureCapacity(destOffset + destLength);
                }
                else
                {
                    // Copy previous entry to the end
                    destOffset = _nextOffset;
                    EnsureCapacity(destOffset + destLength);
                    Buffer.BlockCopy(_buffer, previousOffset, _buffer, destOffset, previousLength);
                }

                // Actually append the new value
                _buffer[destOffset + destLength - 1] = newValue;
                _codeTable[_count++] = (destOffset, destLength);
                _nextOffset = destOffset + destLength;

                if ((_count & (_count - 1)) == 0 && _codeLength < MaxCodeLength)
                    _codeLength++;
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                ArrayPool<(int, int)>.Shared.Return(_codeTable);
            }

            private void EnsureCapacity(int requiredCapacity)
            {
                if (_buffer.Length < requiredCapacity)
                {
                    var newLength = _buffer.Length * 2;
                    var newBuffer = ArrayPool<byte>.Shared.Rent(newLength);
                    Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = newBuffer;
                }
            }
        }
    }
}
