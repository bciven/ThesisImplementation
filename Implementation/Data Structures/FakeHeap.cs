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
        public void AddOrUpdate(double key, UserEvent value)
        {
            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);
            UserEvent newValue;
            if (_sortedSet.TryGetValue(stringKey, out newValue))
            {
                _sortedSet[stringKey].Utility = key;
            }
            else
            {
                _sortedSet.Add(stringKey, value);
            }
        }

        public void Update(double key, UserEvent value)
        {
            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);
            UserEvent newValue;
            if (_sortedSet.TryGetValue(stringKey, out newValue))
            {
                _sortedSet[stringKey].Utility = key;
            }
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
            foreach (var value in _sortedSet.OrderByDescending(x => x.Value.Utility))
            {
                Console.WriteLine("{0,-4}|{1,-5}|{2,-5}", (char)(value.Value.User + 97), (char)(value.Value.Event + 88), value.Value.Utility);
            }
        }


    }
}
