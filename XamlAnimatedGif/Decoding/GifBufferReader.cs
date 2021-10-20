using System;
using System.IO;
using System.Text;

namespace XamlAnimatedGif.Decoding
{
    internal class GifBufferReader
    {
        private readonly byte[] _buffer;

        public GifBufferReader(byte[] buffer)
        {
            _buffer = buffer;
            Position = 0;
        }

        public int Position { get; set; }

        public string ReadString(int length)
        {
            EnsureCanRead(length);
            var value = Encoding.ASCII.GetString(_buffer, Position, length);
            Position += length;
            return value;
        }

        public byte ReadByte()
        {
            EnsureCanRead(1);
            return _buffer[Position++];
        }

        public byte[] ReadBytes(int count)
        {
            EnsureCanRead(count);
            var array = _buffer.AsSpan(Position, count).ToArray();
            Position += count;
            return array;
        }

        public ushort ReadUInt16()
        {
            EnsureCanRead(2);
            var value = BitConverter.ToUInt16(_buffer, Position);
            Position += 2;
            return value;
        }

        public GifColor[] ReadColorTable(int size)
        {
            GifColor[] colorTable = new GifColor[size];
            for (int i = 0; i < size; i++)
            {
                colorTable[i] = ReadColor();
            }

            return colorTable;
        }

        public GifColor ReadColor()
        {
            EnsureCanRead(3);
            byte r = _buffer[Position];
            byte g = _buffer[Position + 1];
            byte b = _buffer[Position + 2];
            Position += 3;
            return new GifColor(r, g, b);
        }

        public void SkipDataBlocks()
        {
            byte len;
            while ((len = ReadByte()) > 0)
            {
                EnsureCanRead(len);
                Position += len;
            }
        }

        public string ReadStringFromDataBlocks()
        {
            var builder = new StringBuilder();
            byte len;
            while ((len = ReadByte()) > 0)
            {
                EnsureCanRead(len);
                var str = Encoding.ASCII.GetString(_buffer, Position, len);
                builder.Append(str);
                Position += len;
            }

            return builder.ToString();
        }

        public byte[] ReadDataBlocks()
        {
            using var ms = new MemoryStream();
            byte len;
            while ((len = ReadByte()) > 0)
            {
                EnsureCanRead(len);
                ms.Write(_buffer, Position, len);
                Position += len;
            }

            return ms.ToArray();
        }

        public ReadOnlySpan<byte> ReadDataBlocks(byte[] destination)
        {
            byte len;
            int destPosition = 0;
            while ((len = ReadByte()) > 0)
            {
                EnsureCanRead(len);
                Buffer.BlockCopy(_buffer, Position, destination, destPosition, len);
                destPosition += len;
                Position += len;
            }

            return destination.AsSpan(0, destPosition);
        }

        private void EnsureCanRead(int length)
        {
            if (Position + length > _buffer.Length)
                throw new EndOfStreamException();
        }
    }
}