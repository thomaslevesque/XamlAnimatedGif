using System;
using System.Buffers;

namespace XamlAnimatedGif.Extensions
{
    public static class ArrayPoolExtensions
    {
        public static BorrowedArray<T> Borrow<T>(this ArrayPool<T> pool, int minimumLength)
        {
            var array = pool.Rent(minimumLength);
            return new BorrowedArray<T>(array, pool);
        }

        public struct BorrowedArray<T> : IDisposable
        {
            private readonly ArrayPool<T> _pool;

            public BorrowedArray(T[] array, ArrayPool<T> pool)
            {
                Array = array;
                _pool = pool;
            }

            public T[] Array { get; private set; }

            public void Dispose()
            {
                _pool.Return(Array);
                Array = null;
            }

            public static implicit operator T[](BorrowedArray<T> borrowed) => borrowed.Array;
        }
    }
}
