using System.Collections.Generic;

namespace ZomboZ.Infrastructure.Cache
{
    public interface ICache<TKey, TValue>
    {
        bool TryGet(TKey key, out TValue value);
        void Set(TKey key, TValue value);
        bool Remove(TKey key);
        void Clear();
        IEnumerable<TValue> GetAllValues();
    }
}
