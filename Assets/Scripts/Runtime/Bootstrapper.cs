using UnityEngine;
using ZomboZ.Core.Ports;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    [DisallowMultipleComponent]
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField]
        int cacheCapacity = 256;

        void Awake()
        {
            // Register a simple string->object LRU cache for demo purposes
            var cache = new InMemoryLruCache<string, object>(cacheCapacity);
            ServiceLocator.Register<ICache<string, object>>(cache);
        }
    }
}
