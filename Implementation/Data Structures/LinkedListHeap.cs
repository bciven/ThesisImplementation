using System;
using System.Linq;
using System.Collections.Generic;

namespace Implementation.Data_Structures
{
    public class LinkedListHeap
    {
        private readonly bool _doublePriority;
        public readonly Dictionary<string, UserEvent> _dictionary;
        public readonly LinkedList<UserEvent> _sortedSet;

        // O(1)
        public UserEvent Max
        {
            get
            {
                var max1 = new UserEvent { Utility = double.MinValue, Priority = double.MinValue };
                var max2 = new UserEvent { Utility = double.MinValue, Priority = double.MinValue };

                max1 = _sortedSet.Last.Value;
                if (_doublePriority)
                {
                    foreach (var pair in _dictionary)
                    {
                        if (Math.Abs(pair.Value.Utility - max1.Utility) < 0.00001 &&
                            pair.Value.Priority > max2.Priority)
                        {
                            max2 = pair.Value;
                        }
                    }
                    max1 = max2;
                }
                return max1;
            }
        }

        //private double FindMedian()
        //{
        //    var n = _sortedSet.Count;
        //    if (n % 2 == 1)
        //    {
        //        var i = n + 1;
        //        return 
        //    }
        //    else
        //    {

        //    }
        //}

        public LinkedListHeap(bool doublePriority)
        {
            throw new NotImplementedException();
            _doublePriority = doublePriority;
            //_sortedSet = new Dictionary<string, UserEvent>();
            _sortedSet = new LinkedList<UserEvent>();
        }

        // O(logn)
        public void AddOrUpdate(double utility, UserEvent newNode)
        {
            newNode.Utility = utility;
            var newNodeKey = CreateKey(newNode.User, newNode.Event);

            UserEvent newValue;
            bool added = false;
            if (_dictionary.TryGetValue(newNodeKey, out newValue))
            {
                _dictionary[newNodeKey].Utility = utility;
                _dictionary[newNodeKey].Priority = newNode.Priority;
            }
            else
            {
                added = true;
                _dictionary.Add(newNodeKey, newNode);
            }

            if (_sortedSet.Count == 0)
            {
                _sortedSet.AddFirst(newNode);
                return;
            }

            LinkedListNode<UserEvent> node;
            bool addedDictionary = false;
            for (node = _sortedSet.First; node != _sortedSet.Last.Next; node = node.Next)
            {
                var key = CreateKey(node.Value.User, node.Value.Event);
                if (!added && key == newNodeKey)
                {
                    node.Value.Utility = utility;
                    //node.Value.Priority = newNode.Priority;
                    return;
                }

                if (added && node.Value.Utility > utility)
                {
                    _sortedSet.AddBefore(node, newNode);
                    addedDictionary = true;
                    break;
                }
            }

            if (added && !addedDictionary)
            {
                _sortedSet.AddLast(newNode);
            }
        }

        public void Update(double utility, UserEvent value)
        {
            value.Utility = utility;
            var stringKey = CreateKey(value.User, value.Event);
            UserEvent newValue;
            if (_dictionary.TryGetValue(stringKey, out newValue))
            {
                _dictionary[stringKey].Utility = utility;
                _dictionary[stringKey].Priority = value.Priority;
            }

            LinkedListNode<UserEvent> node;
            for (node = _sortedSet.First; node != _sortedSet.Last.Next; node = node.Next)
            {
                var key = CreateKey(node.Value.User, node.Value.Event);
                if (key == stringKey)
                {
                    node.Value.Utility = utility;
                    //node.Value.Priority = newNode.Priority;
                    return;
                }
            }
        }

        // O(logn)
        public UserEvent RemoveMax()
        {
            var max = Max;
            var stringKey = CreateKey(max.User, max.Event);
            _dictionary.Remove(stringKey);

            LinkedListNode<UserEvent> node;
            for (node = _sortedSet.First; node != _sortedSet.Last.Next; node = node.Next)
            {
                var key = CreateKey(node.Value.User, node.Value.Event);
                if (key == stringKey)
                {
                    _sortedSet.Remove(node);
                }
            }

            return max;
        }

        // O(logn)
        public void Remove(double utility, UserEvent value)
        {
            var stringKey = CreateKey(value.User, value.Event);
            _dictionary.Remove(stringKey);

            LinkedListNode<UserEvent> node;
            for (node = _sortedSet.First; node != _sortedSet.Last.Next; node = node.Next)
            {
                var key = CreateKey(node.Value.User, node.Value.Event);
                if (key == stringKey)
                {
                    _sortedSet.Remove(node);
                }
            }
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
            foreach (var value in _dictionary.OrderByDescending(x => x.Value.Utility))
            {
                Console.WriteLine("{0,-4}|{1,-5}|{2,-5}", (char)(value.Value.User + 97), (char)(value.Value.Event + 88), value.Value.Utility);
            }
        }


    }
}
