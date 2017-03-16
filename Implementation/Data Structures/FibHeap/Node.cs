using System;

namespace Implementation.Data_Structures.FibHeap
{
    public partial class FibonacciHeap<TKey, TValue> : IPriorityQueue<TKey, TValue>
        where TKey : IComparable 
    {

        /// <summary>
        /// A node object used to store data in the Fibonacci heap.
        /// </summary>
        public class Node : INode<TKey, TValue> 
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public int Degree { get; set; }
            public Node Parent { get; set; }
            public Node Child { get; set; }
            public Node Prev { get; set; }
            public Node Next { get; set; }
            public bool IsMarked { get; set; } 

            /// <summary>
            /// Creates a Fibonacci heap node.
            /// </summary>
            public Node()
            { 
            }

            /// <summary>
            /// Creates a Fibonacci heap node initialised with a key and value.
            /// </summary>
            /// <param name="key">The key to use.</param>
            /// <param name="val">The value to use.</param>
            public Node(TKey key, TValue val) 
            {
                Key = key;
                Value = val;
                Next = this;
                Prev = this;
            }

            public int CompareTo(object other) 
            {
                var casted = other as Node;
                if (casted == null)
                    throw new NotImplementedException("Cannot compare to a non-Node object");
                return this.Key.CompareTo(casted.Key);
            }
        }
    }
}