using System;
using System.Collections.Generic;

namespace Implementation.Data_Structures
{
    public class Heap<K, V>
        where K : IComparable
        where V : UserEvent
    {
        private readonly SortedSet<KeyValuePair<K, V>> _sortedSet;

        // O(1)
        public KeyValuePair<K, V> Min
        {
            get { return _sortedSet.Min; }
        }

        public Heap()
        {
            _sortedSet = new SortedSet<KeyValuePair<K, V>>(new KeyValueComparer<K, V>());
        }

        // O(logn)
        public void Add(K key, V value)
        {
            _sortedSet.Add(new KeyValuePair<K, V>(key, value));
        }

        // O(logn)
        public KeyValuePair<K, V> RemoveMin()
        {
            var min = Min;
            _sortedSet.Remove(min);
            return min;
        }

        // O(logn)
        public void Remove(K key, V value)
        {
            _sortedSet.Remove(new KeyValuePair<K, V>(key, value));
        }

        // O(logn)
        public void UpdateKey(K oldKey, V oldValue, K newKey)
        {
            Remove(oldKey, oldValue);
            Add(newKey, oldValue);
        }

        public bool IsEmpty()
        {
            return _sortedSet.Count == 0;
        }

        public int Count()
        {
            return _sortedSet.Count;
        }

        private class KeyValueComparer<K, V> : IComparer<KeyValuePair<K, V>>
            where K : IComparable
            where V : UserEvent
        {
            public int Compare(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
            {
                var result = x.Key.CompareTo(y.Key);
                if (result == 0)
                {
                    if (x.Value.Event > y.Value.Event)
                    {
                        return 1;
                    }

                    if(x.Value.Event < y.Value.Event)
                    {
                        return -1;
                    }

                    if (x.Value.User > y.Value.User)
                    {
                        return 1;
                    }

                    if (x.Value.User < y.Value.User)
                    {
                        return -1;
                    }

                    return 0;
                }
                return result;
            }
        }
    }
}
