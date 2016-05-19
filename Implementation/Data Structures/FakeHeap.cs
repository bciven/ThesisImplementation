using System;
using System.Linq;
using System.Collections.Generic;

namespace Implementation.Data_Structures
{
    public class FakeHeap
    {
        public readonly List<KeyValuePair<double, UserEvent>> _sortedSet;

        // O(1)
        public KeyValuePair<double, UserEvent> Max
        {
            get
            {
                var max = new KeyValuePair<double, UserEvent>(int.MinValue, null);
                foreach (var pair in _sortedSet)
                {
                    if (pair.Key > max.Key)
                    {
                        max = pair;
                    }
                }
                return max;
            }
        }

        public FakeHeap()
        {
            _sortedSet = new List<KeyValuePair<double, UserEvent>>();
        }

        // O(logn)
        public void Add(double key, UserEvent value)
        {
            if (!_sortedSet.Exists(x => x.Key == key && value.User == x.Value.User && value.Event == x.Value.Event))
            {
                _sortedSet.Add(new KeyValuePair<double, UserEvent>(key, value));
            }
        }

        // O(logn)
        public KeyValuePair<double, UserEvent> RemoveMax()
        {
            var max = Max;
            _sortedSet.Remove(max);
            return max;
        }

        // O(logn)
        public void Remove(double key, UserEvent value)
        {
            _sortedSet.RemoveAll(x=> x.Key == key && x.Value.User == value.User && x.Value.Event == value.Event);
        }

        // O(logn)
        public void UpdateKey(double oldKey, UserEvent oldValue, double newKey)
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

        public void Print()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("User|Event|Value");
            Console.WriteLine("----------------");
            foreach (var value in _sortedSet.OrderByDescending(x=>x.Key))
            {
                Console.WriteLine("{0,-4}|{1,-5}|{2,-5}", value.Value.User, value.Value.Event, value.Key);
            }
        }
    }
}
