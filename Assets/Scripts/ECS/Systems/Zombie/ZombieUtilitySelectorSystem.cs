using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieSpawnSystem))]   // ⬅️ important
public partial struct ZombieUtilitySelectorSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;

        var q = SystemAPI.QueryBuilder()
            .WithAll<ZombieTag, ZombieBlackboard, LocalTransform>()
            .Build();

        using var ents = q.ToEntityArray(Unity.Collections.Allocator.TempJob);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var bb = em.GetComponentData<ZombieBlackboard>(e);
            bb.TimeSinceSeenPlayer += dt;

            float idle = 0.1f;
            float wander = 0.4f;
            float chase = math.saturate(1f - math.saturate(bb.TimeSinceSeenPlayer / 3f));

            int choice = 0; float best = idle;
            if (wander > best) { best = wander; choice = 1; }
            if (chase > best) { best = chase; choice = 2; }

            bool hasIdle = em.HasComponent<IdleTag>(e);
            bool hasWander = em.HasComponent<WanderTag>(e);
            bool hasChase = em.HasComponent<ChaseTag>(e);

            if (choice == 0 && !hasIdle) { em.RemoveComponent<WanderTag>(e); em.RemoveComponent<ChaseTag>(e); em.AddComponent<IdleTag>(e); }
            if (choice == 1 && !hasWander) { em.RemoveComponent<IdleTag>(e); em.RemoveComponent<ChaseTag>(e); em.AddComponent<WanderTag>(e); }
            if (choice == 2 && !hasChase) { em.RemoveComponent<IdleTag>(e); em.RemoveComponent<WanderTag>(e); em.AddComponent<ChaseTag>(e); }

            em.SetComponentData(e, bb);
        }
    }
}
