namespace Implementation.Data_Structures
{
    public interface IHeap
    {
        UserEvent Max { get; }
        void AddOrUpdate(double key, UserEvent value);
        int Count();
        bool IsEmpty();
        void Print();
        void Remove(double key, UserEvent value);
        UserEvent RemoveMax();
        void Update(double key, UserEvent value);
    }
}