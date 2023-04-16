using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NefAndFriends.LevelDesigner
{
    [Serializable]
    public class UnorderedList<T> : IList<T>, IList
    {
        [SerializeField, SerializeReference]
        private T[] arrayData;

        [SerializeField]
        private int count;

        public bool IsSynchronized { get; }
        public object SyncRoot { get; }

        public bool IsFixedSize { get; }

        public bool IsReadOnly => false;

        public UnorderedList(int capacity = 0)
        {
            arrayData = new T[capacity];
            Count = 0;

            IsSynchronized = true;
            SyncRoot = typeof(UnorderedList<T>);
            IsFixedSize = false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return arrayData[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            var i = Count++;
            if (arrayData.Length < Count)
            {
                Array.Resize(ref arrayData, Count);
            }

            arrayData[i] = item;
        }

        public int Add(object value)
        {
            Add((T)value);
            return Count - 1;
        }

        public void Clear()
        {
            Count = 0;
        }

        public bool Contains(object value)
        {
            return Array.IndexOf(arrayData, value) > -1;
        }

        public int IndexOf(object value)
        {
            return Array.IndexOf(arrayData, value);
        }

        public void Insert(int index, object value)
        {
            Add(value);
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public bool Contains(T item)
        {
            return Array.IndexOf(arrayData, item) > -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = arrayIndex; i < array.Length; i++)
            {
                array[i] = this[i];
            }
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

        public void CopyTo(Array array, int index)
        {
            for (var i = index; i < array.Length; i++)
            {
                array.SetValue(this[i], i);
            }
        }

        public int Count
        {
            get => count;
            private set => count = value;
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        public int IndexOf(T item)
        {
            for (var i = 0; i < Count; i++)
            {
                if (arrayData[i].Equals(item))
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
            arrayData[index] = arrayData[--Count];
        }

        public T this[int index]
        {
            get => arrayData[index];
            set => arrayData[index] = value;
        }

        public UnorderedList<T> Clone()
        {
            var @new = new UnorderedList<T>(Count);
            Array.Copy(arrayData, @new.arrayData, Count);
            @new.Count = Count;
            return @new;
        }
    }
}
