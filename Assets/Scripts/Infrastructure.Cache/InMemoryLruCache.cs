using System.Collections.Generic;

namespace ZomboZ.Infrastructure.Cache
{
    public class InMemoryLruCache<TKey, TValue> : ICache<TKey, TValue>
    {
        readonly int capacity;
        readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> map;
        readonly LinkedList<KeyValuePair<TKey, TValue>> list;

        public InMemoryLruCache(int capacity)
        {
            if (capacity <= 0) throw new System.ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public void Set(TKey key, TValue value)
        {
            if (map.TryGetValue(key, out var node))
            {
                list.Remove(node);
            }
            else if (map.Count >= capacity)
            {
                var last = list.Last;
                if (last != null)
                {
                    list.RemoveLast();
                    map.Remove(last.Value.Key);
                }
            }

            var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
            list.AddFirst(newNode);
            map[key] = newNode;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if (map.TryGetValue(key, out var node))
            {
                list.Remove(node);
                list.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public bool Remove(TKey key)
        {
            if (map.TryGetValue(key, out var node))
            {
                list.Remove(node);
                return map.Remove(key);
            }
            return false;
        }

        public void Clear()
        {
            map.Clear();
            list.Clear();
        }

        public IEnumerable<TValue> GetAllValues()
        {
            foreach (var kvp in list)
            {
                yield return kvp.Value;
            }
        }
    }
}
