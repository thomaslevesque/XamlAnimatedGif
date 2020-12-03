using System;

namespace XamlAnimatedGif.Decompression
{
    class BitReader
    {
        private readonly Memory<byte> _buffer;

        public BitReader(Memory<byte> buffer)
        {
            _buffer = buffer;
        }

        private int _bytePosition = -1;
        private int _bitPosition;
        private int _currentValue = -1;

        public bool TryReadBits(int bitCount, out int value)
        {
            // The following code assumes it's running on a little-endian architecture.
            // It's probably safe to assume it will always be the case, because:
            // - Windows only supports little-endian architectures: x86/x64 and ARM (which supports
            //   both endiannesses, but Windows on ARM is always in little-endian mode)
            // - No platforms other than Windows support XAML applications
            // If the situation changes, this code will have to be updated.

            value = 0;
            if (_bytePosition == -1)
            {
                _bytePosition = 0;
                _bitPosition = 0;
                _currentValue = ReadInt32();
            }
            else if (_bytePosition >= _buffer.Length)
            {
                return false;
            }
            else if (bitCount > 32 - _bitPosition)
            {
                _bytePosition += (_bitPosition >> 3);
                _bitPosition &= 0x07;
                _currentValue = ReadInt32() >> _bitPosition;
            }
            int mask = (1 << bitCount) - 1;
            value = _currentValue & mask;
            _currentValue >>= bitCount;
            _bitPosition += bitCount;
            return true;
        }

        private int ReadInt32()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                if (_bytePosition + i >= _buffer.Length)
                    break;
                value |= _buffer.Span[_bytePosition + i] << (i << 3);
            }
            return value;
        }
    }
}
