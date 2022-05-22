using System;
using System.Collections.Generic;

namespace OpenUGD.AsyncBundles
{
    internal class ListPool<T>
    {
        private static readonly Stack<List<T>> Stack = new Stack<List<T>>(8);
        private static readonly object Lock = new object();

        public static List<T> Pop()
        {
            return PopList();
        }

        public static List<T> Pop(int capacity)
        {
            return PopList(capacity);
        }

        public static List<T> Pop(IEnumerable<T> value)
        {
            var result = PopList();
            result.AddRange(value);
            return result;
        }

        public static void Push(List<T> list)
        {
            if (list != null)
            {
                PushList(list);
            }
        }

        private static List<T> PopList()
        {
            lock (Lock)
            {
                if (Stack.Count == 0)
                {
                    return new List<T>(8);
                }

                return Stack.Pop();
            }
        }

        private static List<T> PopList(int capacity)
        {
            lock (Lock)
            {
                if (Stack.Count == 0) return new List<T>(capacity);
                var result = Stack.Pop();
                if (result.Capacity < capacity) result.Capacity = capacity;
                return result;
            }
        }

        private static void PushList(List<T> list)
        {
            list.Clear();
            lock (Lock)
            {
                if (Stack.Contains(list))
                {
                    throw new ArgumentException();
                }

                Stack.Push(list);
            }
        }
    }
}
