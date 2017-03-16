using System;

namespace Implementation.Data_Structures.FibHeap 
{
    public interface INode<TKey, TValue> : IComparable 
        where TKey : IComparable 
    {
        TKey Key { get; }
        TValue Value { get; }
    }
}