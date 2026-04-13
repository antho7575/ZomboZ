using Unity.Entities;
using Unity.Mathematics;


namespace ZomboZ.Runtime
{
    public partial class SimpleZombieSpawnerSystem : SystemBase
    {
        double _lastSpawnTime = 0.0;

        protected override void OnCreate()
        {
            RequireForUpdate<ZombieSpawnSettings>();
        }

        protected override void OnUpdate()
        {
            var em = EntityManager;

            // timing
            var now = SystemAPI.Time.ElapsedTime;
            var settings = SystemAPI.GetSingleton<ZombieSpawnSettings>();
            if (now - _lastSpawnTime < settings.SpawnInterval)
            {
                return;
            }
            
            _lastSpawnTime = now;

            // Get player position
            float3 center = PlayerService.GetPlayerPosition();

            // Use cache to restore nearby zombies first
            var nearby = ZombieCacheService.QueryNear(center, settings.SpawnRadius);

            // Spawn restored zombies
            for (int i = 0; i < nearby.Count; i++)
            {
                var r = nearby[i];
                var request = new ZombieCreateRequest
                {
                    Prefab = settings.Prefab,
                    Id = r.Id,
                    Position = new float3(r.PosX, r.PosY, r.PosZ),
                    Rotation = quaternion.EulerXYZ(0, r.RotationY, 0),
                    Scale = 1f,
                    MoveSpeed = 1f,
                    Hunger = r.Health,
                    Velocity = float3.zero,
                    DesiredVelocity = float3.zero,
                    TimeSinceSeenPlayer = 999f,
                    WithWander = true,
                    WithAnimation = true
                };

                // Spawn zombie entity from cache data
                ZombieEntityFactory.CreateZombie(em, request);

                // Mark record as spawned so it won't be chosen again until despawned from cache
                r.IsSpawned = true;
                ZombieCacheService.AddOrUpdate(r);
            }
        }
    }
}
