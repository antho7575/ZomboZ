using System.Collections.Generic;
using UnityEngine;

namespace ZomboZ.Runtime
{
    public static class CacheBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            try
            {
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
