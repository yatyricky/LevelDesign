using System;
using System.Collections;
using System.Collections.Generic;

namespace NefAndFriends.LevelDesigner
{
    public class UnorderedList<T> : IList<T>
    {
        private T[] _array;

        public UnorderedList(int capacity = 0)
        {
            _array = new T[capacity];
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return _array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            var i = Count++;
            if (_array.Length < Count)
            {
                Array.Resize(ref _array, Count);
            }

            _array[i] = item;
        }

        public void Clear()
        {
            Count = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            var i = IndexOf(item);
            if (i == -1)
            {
                return false;
            }

            RemoveAt(i);
            return true;
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public int IndexOf(T item)
        {
            for (var i = 0; i < Count; i++)
            {
                if (_array[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            _array[index] = _array[--Count];
        }

        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public UnorderedList<T> Clone()
        {
            var @new = new UnorderedList<T>(Count);
            Array.Copy(_array, @new._array, Count);
            @new.Count = Count;
            return @new;
        }
    }
}
