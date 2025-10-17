using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class SimpleZombieSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ZombieStreamConfig>();
    }

    protected override void OnUpdate()
    {
        var em = EntityManager;
        var cfg = SystemAPI.GetSingleton<ZombieStreamConfig>();

        // Get player position safely (no RefRO / no SystemAPI.Query foreach)
        float3 center = float3.zero;
        var playerQ = GetEntityQuery(
            ComponentType.ReadOnly<PlayerTag>(),
            ComponentType.ReadOnly<LocalTransform>());

        if (!playerQ.IsEmpty)
        {
            using var tf = playerQ.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            center = tf[0].Position;
        }

        // Spawn N zombies uniformly in a rectangle around 'center'
        var rng = new Unity.Mathematics.Random(cfg.RandomSeed);
        for (int i = 0; i < cfg.TotalZombies; i++)
        {
            float3 pos = new float3(
                center.x + rng.NextFloat(-cfg.HalfExtents.x, cfg.HalfExtents.x),
                center.y,
                center.z + rng.NextFloat(-cfg.HalfExtents.y, cfg.HalfExtents.y));

            var request = new ZombieCreateRequest
            {
                Prefab = cfg.Prefab,
                Position = pos,
                Rotation = quaternion.identity,
                Scale = 1f,

                MoveSpeed = 1f,
                Hunger = 0f,
                Velocity = float3.zero,
                DesiredVelocity = float3.zero,
                TimeSinceSeenPlayer = 999f,

                WithWander = true,
                WithAnimation = true
            };

            ZombieEntityFactory.CreateZombie(em, request);
        }

        // one-shot
        Enabled = false;
    }
}
