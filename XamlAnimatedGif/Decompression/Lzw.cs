using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace XamlAnimatedGif.Decompression
{
    public static class Lzw
    {
        private const int MaxCodeLength = 12;

        public static void Decompress(ReadOnlySpan<byte> dataBlocks, Stream destination, int minimumCodeLength)
        {
            var bitReader = new LzwBitReader(dataBlocks);
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
            private byte _codeLength;

            public CodeTable(int minimumCodeLength)
            {
                _minimumCodeLength = minimumCodeLength;
                _codeLength = (byte)(_minimumCodeLength + 1);
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
                _codeLength = (byte)(_minimumCodeLength + 1);
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
                [DebuggerStepThrough]
                get
                {
                    return _table[index];
                }
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [DebuggerStepThrough]
                get => _count;
            }

            public byte CodeLength
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [DebuggerStepThrough]
                get => _codeLength;
            }
        }
    }
}