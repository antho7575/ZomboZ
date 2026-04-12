using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    public partial class ZombieDespawnSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<ZombieSpawnSettings>();
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<ZombieSpawnSettings>()) return;
            var settings = SystemAPI.GetSingleton<ZombieSpawnSettings>();

            // Get player position via scene object (fallback origin)
            float3 center = float3.zero;
            var playerGo = UnityEngine.GameObject.FindWithTag("Player");
            if (playerGo == null)
                playerGo = UnityEngine.GameObject.Find("Player");
            if (playerGo != null)
            {
                var p = playerGo.transform.position;
                center = new float3(p.x, p.y, p.z);
            }

            var em = EntityManager;
            var despawnSq = settings.DespawnDistance * settings.DespawnDistance;

            // Iterate zombies and despawn those far from player
            var query = GetEntityQuery(ComponentType.ReadOnly<ZombieTag>(), ComponentType.ReadOnly<LocalTransform>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            using var transforms = query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                var t = transforms[i];
                var dx = t.Position.x - center.x;
                var dz = t.Position.z - center.z;
                if (dx * dx + dz * dz > despawnSq)
                {
                    var r = new ZombieCacheModel
                    {
                        Id = System.Guid.NewGuid(),
                        PosX = t.Position.x,
                        PosY = t.Position.y,
                        PosZ = t.Position.z,
                        RotationY = 0f,
                        Health = 100,
                        IsSpawned = false
                    };
                    ZombieCacheService.AddOrUpdate(r);
                    ZombiePersistenceService.AddOrUpdate(r);

                    em.DestroyEntity(e);
                }
            }
        }
    }
}
