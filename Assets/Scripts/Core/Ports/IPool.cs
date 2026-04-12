namespace ZomboZ.Core.Ports
{
    public interface IPool<T>
    {
        T Rent();
        void Return(T item);
        int Count { get; }
    }
}
