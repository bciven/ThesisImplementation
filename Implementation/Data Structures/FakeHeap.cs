using System;
using System.Linq;
using System.Collections.Generic;

namespace Implementation.Data_Structures
{
    public class FakeHeap
    {
        public readonly Dictionary<string, UserEvent> _sortedSet;

        // O(1)
        public UserEvent Max
        {
            get
            {
                var max = new UserEvent { Utility = double.MinValue };

                foreach (var pair in _sortedSet)
                {
                    if (pair.Value.Utility > max.Utility)
                    {
                        max = pair.Value;
                    }
                }
                return max;
            }
        }

        public FakeHeap()
        {
            _sortedSet = new Dictionary<string, UserEvent>();
        }

        // O(logn)
        public void Add(double key, UserEvent value)
        {
            value.Utility = key;
            _sortedSet.Add(CreateKey(value.User, value.Event), value);
        }

        // O(logn)
        public UserEvent RemoveMax()
        {
            var max = Max;
            _sortedSet.Remove(CreateKey(max.User, max.Event));
            return max;
        }

        // O(logn)
        public void Remove(double key, UserEvent value)
        {
            _sortedSet.Remove(CreateKey(value.User, value.Event));
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

        private string CreateKey(int user, int @event)
        {
            return user + "-" + @event;
        }

        public void Print()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("User|Event|Value");
            Console.WriteLine("----------------");
            foreach (var value in _sortedSet.OrderByDescending(x => x.Key))
            {
                Console.WriteLine("{0,-4}|{1,-5}|{2,-5}", (char)(value.Value.User + 97), (char)(value.Value.Event + 88), value.Key);
            }
        }
    }
}
