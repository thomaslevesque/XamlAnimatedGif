using System;
using System.IO;

namespace XamlAnimatedGif.Decompression
{
    internal ref struct LzwBitReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _bytePosition;
        private byte _remainingBits;
        private int _bitBuffer;
        private int _currentBlockEnd;

        public LzwBitReader(ReadOnlySpan<byte> buffer) : this()
        {
            _buffer = buffer;
        }

        public int ReadBits(byte bitCount)
        {
            // The following code assumes it's running on a little-endian architecture.
            // It's probably safe to assume it will always be the case, because:
            // - Windows only supports little-endian architectures: x86/x64 and ARM (which supports
            //   both endiannesses, but Windows on ARM is always in little-endian mode)
            // - No platforms other than Windows support XAML applications
            // If the situation changes, this code will have to be updated.

            while (bitCount > _remainingBits)
            {
                ReadNextByte();
            }
            
            int mask = (1 << bitCount) - 1;
            int value = _bitBuffer & mask;
            _bitBuffer >>= bitCount;
            _remainingBits -= bitCount;
            return value;
        }

        private void ReadNextByte()
        {
            if (_bytePosition >= _buffer.Length)
                throw new EndOfStreamException();
            if (_bytePosition == _currentBlockEnd)
            {
                // Read length of block and update end of block position
                byte len = _buffer[_bytePosition++];
                _currentBlockEnd = _bytePosition + len;
            }
            var b = _buffer[_bytePosition++];
            _bitBuffer |= b << _remainingBits; 
            _remainingBits += 8;
        }
    }
}