using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    public static class ZombieCacheService
    {
        static readonly Dictionary<Guid, ZombieCacheModel> _cache = new Dictionary<Guid, ZombieCacheModel>();

        public static void AddOrUpdate(ZombieCacheModel r)
        {
            r.LastSeenTicks = System.DateTime.UtcNow.Ticks;
            _cache[r.Id] = r;
        }

        public static void Remove(Guid id)
        {
            if (_cache.ContainsKey(id))
                _cache.Remove(id);
        }

        public static List<ZombieCacheModel> QueryNear(float3 center, float radius)
        {
            var sq = radius * radius;
            var list = new List<ZombieCacheModel>();
            foreach (var kv in _cache.Values)
            {
                if (kv.IsSpawned) continue;

                var dx = kv.PosX - center.x;
                var dz = kv.PosZ - center.z;
                if (dx * dx + dz * dz <= sq)
                    list.Add(kv);
            }
            return list;
        }

        public static List<ZombieCacheModel> All() => _cache.Values.ToList();
    }
}
