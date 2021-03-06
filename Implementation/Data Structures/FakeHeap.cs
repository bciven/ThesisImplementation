﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace Implementation.Data_Structures
{
    public class FakeHeap : IHeap
    {
        private readonly bool _doublePriority;
        public readonly Dictionary<string, UserEvent> _sortedSet;
        //private string _maxKey;

        // O(1)
        public UserEvent Max
        {
            get
            {
                var max1 = new UserEvent { Utility = double.MinValue, Priority = double.MinValue };

                foreach (var pair in _sortedSet)
                {
                    if (pair.Value.Utility > max1.Utility)
                    {
                        max1 = pair.Value;
                    }
                }

                return max1;
            }
        }

        public FakeHeap()
        {
            _sortedSet = new Dictionary<string, UserEvent>();
        }

        // O(logn)
        public void AddOrUpdate(double key, UserEvent value)
        {
            if (Double.IsNaN(key))
            {
                throw new Exception("Key is Not an number");
            }

            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);

            //if (_maxKey == null)
            //{
            //    _maxKey = stringKey;
            //}
            //else
            //{
            //    if (_sortedSet[_maxKey].Utility < value.Utility)
            //    {
            //        _maxKey = stringKey;
            //    }
            //}

            UserEvent newValue;
            if (_sortedSet.TryGetValue(stringKey, out newValue))
            {
                _sortedSet[stringKey].Utility = key;
                //_sortedSet[stringKey].Priority = value.Priority;
            }
            else
            {
                _sortedSet.Add(stringKey, value);
            }
        }

        public void Update(double key, UserEvent value)
        {
            if (Double.IsNaN(key))
            {
                throw new Exception("Key is Not an number");
            }

            value.Utility = key;
            var stringKey = CreateKey(value.User, value.Event);
            UserEvent newValue;

            //if (_maxKey == null)
            //{
            //    _maxKey = stringKey;
            //}
            //else
            //{
            //    if (_sortedSet[_maxKey].Utility < value.Utility)
            //    {
            //        _maxKey = stringKey;
            //    }
            //}

            if (_sortedSet.TryGetValue(stringKey, out newValue))
            {
                _sortedSet[stringKey].Utility = key;
                //_sortedSet[stringKey].Priority = value.Priority;
            }
        }

        // O(logn)
        public UserEvent RemoveMax()
        {
            var max = Max;
            var key = CreateKey(max.User, max.Event);
            _sortedSet.Remove(key);
            return max;
        }

        // O(logn)
        public void Remove(double key, UserEvent value)
        {
            if (Double.IsNaN(key))
            {
                throw new Exception("Key is Not an number");
            }

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

        public void RemoveAll()
        {
            _sortedSet.Clear();
        }
    }
}
