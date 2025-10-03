using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ZombieSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;

        var q = SystemAPI.QueryBuilder().WithAll<ZombieSpawner>().Build();
        using var spawners = q.ToEntityArray(Allocator.TempJob);
        if (spawners.Length == 0) return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        uint seed = 0xABCDEFu ^ (uint)(SystemAPI.Time.ElapsedTime * 1000.0);
        var rnd = new Unity.Mathematics.Random(seed == 0 ? 1u : seed);

        for (int si = 0; si < spawners.Length; si++)
        {
            var e = spawners[si];
            var sp = em.GetComponentData<ZombieSpawner>(e);
            if (sp.Prefab == Entity.Null) continue;

            for (int i = 0; i < sp.Count; i++)
            {
                var z = ecb.Instantiate(sp.Prefab);

                float3 pos = new float3(
                    rnd.NextFloat(-sp.Area.x, sp.Area.x),
                    0f,
                    rnd.NextFloat(-sp.Area.y, sp.Area.y)
                );

                ecb.SetComponent(z, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
                ecb.AddComponent<ZombieTag>(z);
                ecb.AddComponent(z, new Velocity { Value = float3.zero });
                ecb.AddComponent(z, new MoveSpeed { Value = sp.Speed });

                // 🔹 add these so the selector & steering can see them:
                ecb.AddComponent(z, new DesiredVelocity { Value = float3.zero });
                ecb.AddComponent(z, new ZombieBlackboard { TimeSinceSeenPlayer = 999f, LastKnownPlayerPos = pos, Hunger = 0f });

                // (optional) start everyone as Wander so you see behavior immediately
                // ecb.AddComponent<WanderTag>(z);
            }

            ecb.RemoveComponent<ZombieSpawner>(e); // one-shot
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
