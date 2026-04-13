using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using System.Linq;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    /// <summary>
    /// Despawns zombies that are outside the spawn radius and saves them to cache.
    /// Uses cache spatial query + entity registry for O(1) lookups.
    /// </summary>
    public partial class ZombieDespawnSystem : SystemBase
    {
        Throttle _throttle = new Throttle(0.5);

        protected override void OnCreate()
        {
            RequireForUpdate<ZombieSpawnSettings>();
        }

        protected override void OnUpdate()
        {
            var settings = SystemAPI.GetSingleton<ZombieSpawnSettings>();

            // Throttle: only check every 0.5 seconds (not every frame!)
            if (!_throttle.ShouldExecute(SystemAPI.Time.ElapsedTime))
                return;

            // Get player position
            float3 playerPos = PlayerService.GetPlayerPosition();

            var em = EntityManager;

            // Query cache for zombies that are outside the spawn radius and currently spawned
            var toDespawn = ZombieCacheService.QueryOutsideAndSpawned(playerPos, settings.SpawnRadius);

            
            if (toDespawn.Count == 0)
                return;

            // Despawn each zombie outside the radius
            foreach (var cached in toDespawn)
            {
                // Find entity by GUID using registry (O(1) lookup!)
                if (!ZombieEntityRegistry.TryGetEntity(cached.Id, out var targetEntity))
                {
                    // Entity not found in registry, mark as not spawned in cache
                    cached.IsSpawned = false;
                    ZombieCacheService.AddOrUpdate(cached);
                    continue;
                }

                if (!em.Exists(targetEntity))
                {
                    cached.IsSpawned = false;
                    ZombieCacheService.AddOrUpdate(cached);
                    ZombieEntityRegistry.Unregister(cached.Id);
                    continue;
                }

                // Get latest state before despawning
                var transform = em.GetComponentData<LocalTransform>(targetEntity);
                var blackboard = em.GetComponentData<ZombieBlackboard>(targetEntity);

                // Get rotation Y from quaternion
                var euler = math.degrees(math.atan2(
                    2f * (transform.Rotation.value.w * transform.Rotation.value.y + transform.Rotation.value.x * transform.Rotation.value.z),
                    1f - 2f * (transform.Rotation.value.y * transform.Rotation.value.y + transform.Rotation.value.z * transform.Rotation.value.z)));

                // Update cache with latest state
                cached.PosX = transform.Position.x;
                cached.PosY = transform.Position.y;
                cached.PosZ = transform.Position.z;
                cached.RotationY = euler;
                cached.Health = (int)blackboard.Hunger;
                cached.IsSpawned = false;  // Mark as despawned
                ZombieCacheService.AddOrUpdate(cached);

                // Unregister from registry before destroying
                ZombieEntityRegistry.Unregister(cached.Id);

                // Destroy the entity
                em.DestroyEntity(targetEntity);
            }
        }
    }
}
