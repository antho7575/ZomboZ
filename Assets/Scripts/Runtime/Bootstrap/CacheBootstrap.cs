using System;
using System.Collections.Generic;
using UnityEngine;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    public static class CacheBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            try
            {
                var zombieCache = new InMemoryLruCache<Guid, ZombieCacheModel>(1000);

                ServiceLocator.Register<ICache<Guid, ZombieCacheModel>>(zombieCache);

                ZombieCacheService.Initialize(zombieCache);

                var all = ZombiePersistenceService.LoadAll();
                foreach (var r in all)
                {
                    ZombieCacheService.AddOrUpdate(r);
                }
                Debug.Log($"Zombie cache loaded: {all.Count} records");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load zombie cache: {ex.Message}");
            }
        }
    }
}
