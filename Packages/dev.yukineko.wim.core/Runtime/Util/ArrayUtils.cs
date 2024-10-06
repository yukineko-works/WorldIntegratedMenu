using System;

namespace yukineko.WorldIntegratedMenu
{
    public static class ArrayUtils
    {
        public static T[] Add<T>(T[] source, T item)
        {
            var length = source.Length;
            var newArray = new T[length + 1];
            Array.Copy(source, newArray, length);
            newArray[length] = item;
            return newArray;
        }

        public static T[] RemoveAt<T>(T[] source, int index)
        {
            var length = source.Length;
            if (index >= length || index < 0) { return source; }

            int newSize = length - 1;
            var newArray = new T[newSize];

            if (index == 0)
            {
                Array.Copy(source, 1, newArray, 0, newSize);
            }
            else if (index == newSize)
            {
                Array.Copy(source, 0, newArray, 0, newSize);
            }
            else
            {
                Array.Copy(source, 0, newArray, 0, index);
                Array.Copy(source, index + 1, newArray, index, newSize - index);
            }

            return newArray;
        }

        public static bool Contains<T>(T[] source, T item)
        {
            return Array.IndexOf(source, item) != -1;
        }

        public static T[] Concat<T>(T[] source, T[] items)
        {
            var length = source.Length;
            var newLength = length + items.Length;
            var newArray = new T[newLength];
            Array.Copy(source, newArray, length);
            Array.Copy(items, 0, newArray, length, items.Length);
            return newArray;
        }

        public static T[] Clear<T>(T[] source)
        {
            return new T[0];
        }
    }
}