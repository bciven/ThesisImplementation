using System;
using System.Linq;
using System.Collections.Generic;
using Implementation.Data_Structures.FibHeap;

namespace Implementation.Data_Structures
{
    public class FiboHeap: IHeap
    {
        private readonly bool _doublePriority;
        public readonly FibonacciHeap<double, UserEvent> _heap;
        public Dictionary<string, double> _values; 
        //private string _maxKey;

        // O(1)
        public UserEvent Max
        {
            get
            {
                //var min = _heap.FindMinimum().Value;
                //var ue = new UserEvent(min.User, min.Event, min.Utility * -1);
                return new UserEvent(0,0); //ue;
            }
        }

        public FiboHeap()
        {
            _heap = new FibonacciHeap<double, UserEvent>();
            _values = new Dictionary<string, double>();
        }

        // O(logn)
        public void AddOrUpdate(double key, UserEvent userEvent)
        {
            if (Double.IsNaN(key))
            {
                throw new Exception("Key is Not an number");
            }
            userEvent.Utility = key;
            var value = userEvent.Copy();

            key *= -1;
            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);

            var newNode = new FibonacciHeap<double, UserEvent>.Node(key, value);
            double oldKey;
            if (_values.TryGetValue(stringKey, out oldKey))
            {
                if (key > oldKey)
                {
                    throw new Exception("Not acceptable");
                }

                _heap.DecreaseKey(newNode, key);
                _values[stringKey] = key;
            }
            else
            {
                _heap.Insert(key, value);
                _values.Add(stringKey, key);
            }
        }

        public void Update(double key, UserEvent userEvent)
        {
            if (Double.IsNaN(key))
            {
                throw new Exception("Key is Not an number");
            }
            userEvent.Utility = key;
            var value = userEvent.Copy();
            key *= -1;
            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);

            var newNode = new FibonacciHeap<double, UserEvent>.Node(key, value);
            double newValue;
            if (_values.TryGetValue(stringKey, out newValue))
            {

                _heap.DecreaseKey(newNode, key);
                _values[stringKey] = key;
            }
        }

        // O(logn)
        public UserEvent RemoveMax()
        {
            var min =_heap.ExtractMinimum();
            var key = CreateKey(min.Value.User, min.Value.Event);

            _values.Remove(key);
            var userEvent = min.Value.Copy();
            userEvent.Utility *= -1;

            return userEvent;
        }

        public bool IsEmpty()
        {
            return _heap.Size == 0;
        }

        public int Count()
        {
            return _heap.Size;
        }

        private string CreateKey(int user, int @event)
        {
            return user + "-" + @event;
        }

        public void Print()
        {
        }

        public void Remove(double key, UserEvent value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAll()
        {
            _heap.Clear();
        }
    }
}
