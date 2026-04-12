using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    public static class ZombieCacheService
    {
        static ICache<Guid, ZombieCacheModel> _cache;

        public static void Initialize(ICache<Guid, ZombieCacheModel> cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public static void AddOrUpdate(ZombieCacheModel r)
        {
            r.LastSeenTicks = System.DateTime.UtcNow.Ticks;
            _cache.Set(r.Id, r);
        }

        public static void Remove(Guid id)
        {
            _cache.Remove(id);
        }

        public static List<ZombieCacheModel> QueryNear(float3 center, float radius)
        {
            var sq = radius * radius;
            var list = new List<ZombieCacheModel>();
            foreach (var model in _cache.GetAllValues())
            {
                if (model.IsSpawned) continue;

                var dx = model.PosX - center.x;
                var dz = model.PosZ - center.z;
                if (dx * dx + dz * dz <= sq)
                    list.Add(model);
            }
            return list;
        }

        public static List<ZombieCacheModel> All() => _cache.GetAllValues().ToList();
    }
}
