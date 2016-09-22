//using System;
//using System.Linq;
//using System.Collections.Generic;

//namespace Implementation.Data_Structures
//{
//    public class LinkedListHeap
//    {
//        private readonly bool _doublePriority;
//        //public readonly Dictionary<string, UserEvent> _sortedSet;
//        public readonly LinkedList<UserEvent> _sortedSet;

//        // O(1)
//        public UserEvent Max
//        {
//            get
//            {
//                var max1 = new UserEvent { Utility = double.MinValue, Priority = double.MinValue };
//                var max2 = new UserEvent { Utility = double.MinValue, Priority = double.MinValue };

//                foreach (var pair in _sortedSet)
//                {
//                    if (pair.Value.Utility > max1.Utility)
//                    {
//                        max1 = pair.Value;
//                    }
//                }
//                if (_doublePriority)
//                {
//                    foreach (var pair in _sortedSet)
//                    {
//                        if (Math.Abs(pair.Value.Utility - max1.Utility) < 0.00001 &&
//                            pair.Value.Priority > max2.Priority)
//                        {
//                            max2 = pair.Value;
//                        }
//                    }
//                    max1 = max2;
//                }
//                return max1;
//            }
//        }

//        public FakeHeap(bool doublePriority)
//        {
//            _doublePriority = doublePriority;
//            //_sortedSet = new Dictionary<string, UserEvent>();
//            _sortedSet = new LinkedList<UserEvent>();
//        }

//        // O(logn)
//        public void AddOrUpdate(double utility, UserEvent newNode)
//        {
//            newNode.Utility = utility;
//            //var stringKey = CreateKey(value.User, value.Event);

//            //UserEvent newValue;
//            //if (_sortedSet.TryGetValue(stringKey, out newValue))
//            //{
//            //    _sortedSet[stringKey].Utility = key;
//            //    _sortedSet[stringKey].Priority = value.Priority;
//            //}
//            //else
//            //{
//            //    _sortedSet.Add(stringKey, value);
//            //}
//            if (_sortedSet.Count == 0)
//            {
//                _sortedSet.AddFirst(newNode);
//                return;
//            }

//            LinkedListNode<UserEvent> node;
//            bool added = false;
//            var newNodeKey = CreateKey(newNode.User, newNode.Event);
//            for (node = _sortedSet.First; node != _sortedSet.Last.Next; node = node.Next)
//            {
//                var key = CreateKey(node.Value.User, node.Value.Event);
//                if (key == newNodeKey)
//                {
//                    node.Value.Utility = utility;
//                    //node.Value.Priority = newNode.Priority;
//                    return;
//                }
//                if (node.Value.Utility > utility)
//                {
//                    _sortedSet.AddBefore(node, newNode);
//                    added = true;
//                    break;
//                }
//            }
//            if (!added)
//            {
//                _sortedSet.AddLast(newNode);
//            }
//        }

//        public void Update(double key, UserEvent value)
//        {
//            value.Utility = key;
//            var stringKey = CreateKey(value.User, value.Event);
//            UserEvent newValue;
//            if (_sortedSet.TryGetValue(stringKey, out newValue))
//            {
//                _sortedSet[stringKey].Utility = key;
//                _sortedSet[stringKey].Priority = value.Priority;
//            }
//        }

//        // O(logn)
//        public UserEvent RemoveMax()
//        {
//            var max = Max;
//            _sortedSet.Remove(CreateKey(max.User, max.Event));
//            return max;
//        }

//        // O(logn)
//        public void Remove(double key, UserEvent value)
//        {
//            _sortedSet.Remove(CreateKey(value.User, value.Event));
//        }

//        // O(logn)


//        public bool IsEmpty()
//        {
//            return _sortedSet.Count == 0;
//        }

//        public int Count()
//        {
//            return _sortedSet.Count;
//        }

//        private string CreateKey(int user, int @event)
//        {
//            return user + "-" + @event;
//        }

//        public void Print()
//        {
//            Console.WriteLine();
//            Console.WriteLine();
//            Console.WriteLine("User|Event|Value");
//            Console.WriteLine("----------------");
//            foreach (var value in _sortedSet.OrderByDescending(x => x.Value.Utility))
//            {
//                Console.WriteLine("{0,-4}|{1,-5}|{2,-5}", (char)(value.Value.User + 97), (char)(value.Value.Event + 88), value.Value.Utility);
//            }
//        }


//    }
//}
