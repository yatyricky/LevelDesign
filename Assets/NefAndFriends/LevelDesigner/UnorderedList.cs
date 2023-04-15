using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    [Serializable]
    public class UnorderedList<T> : IList<T>
    {
        [SerializeField, SerializeReference]
        private T[] array;

        public UnorderedList(int capacity = 0)
        {
            array = new T[capacity];
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            var i = Count++;
            if (array.Length < Count)
            {
                Array.Resize(ref array, Count);
            }

            array[i] = item;
        }

        public void Clear()
        {
            Count = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] targetArray, int arrayIndex)
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

        [SerializeField]
        private int count;

        public int Count
        {
            get => count;
            private set => count = value;
        }

        public bool IsReadOnly => false;

        public int IndexOf(T item)
        {
            for (var i = 0; i < Count; i++)
            {
                if (array[i].Equals(item))
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
            array[index] = array[--Count];
        }

        public T this[int index]
        {
            get => array[index];
            set => array[index] = value;
        }

        public UnorderedList<T> Clone()
        {
            var @new = new UnorderedList<T>(Count);
            Array.Copy(array, @new.array, Count);
            @new.Count = Count;
            return @new;
        }
    }
}
