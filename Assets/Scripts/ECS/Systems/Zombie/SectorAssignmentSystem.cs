using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieSpawnSystem))] // make sure zombies exist first
public partial struct SectorAssignmentSystem : ISystem
{
    private EntityQuery _q;
    private ComponentLookup<LocalTransform> _xfRO;

    public void OnCreate(ref SystemState s)
    {
        _q = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform>()
            .Build(ref s);

        _xfRO = s.GetComponentLookup<LocalTransform>(isReadOnly: true);
    }

    public void OnUpdate(ref SystemState s)
    {
        if (!SystemAPI.TryGetSingleton<ObserverSettings>(out var obs))
            return;

        _xfRO.Update(ref s);

        var em = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        using var ents = _q.ToEntityArray(Allocator.Temp);
        using var xfs = _q.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        // sector index = floor(pos / SectorSize)
        float inv = 1f / math.max(1e-3f, obs.SectorSize);

        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var p = xfs[i].Position;

            int2 sector = new int2(
                (int)math.floor(p.x * inv),
                (int)math.floor(p.z * inv)
            );

            // IMPORTANT:
            // - AddComponent is structural -> via ECB
            // - SetComponentData is not structural -> can call directly
            if (!em.HasComponent<Sector>(e))
            {
                ecb.AddComponent(e, new Sector { XY = sector });
            }
            else
            {
                em.SetComponentData(e, new Sector { XY = sector });
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
