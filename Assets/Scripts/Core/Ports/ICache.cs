namespace ZomboZ.Core.Ports
{
    public interface ICache<TKey, TValue>
    {
        bool TryGet(TKey key, out TValue value);
        void Set(TKey key, TValue value);
        bool Remove(TKey key);
        void Clear();
    }
}
