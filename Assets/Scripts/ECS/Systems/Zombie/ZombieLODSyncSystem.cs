using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SectorAssignmentSystem))]
public partial struct ZombieLODSyncSystem : ISystem
{
    private EntityQuery _q;
    private ComponentLookup<LocalTransform> _xf;

    public void OnCreate(ref SystemState s)
    {
        _q = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform>()
            .Build(ref s);
        _xf = s.GetComponentLookup<LocalTransform>(true);
    }

    public void OnUpdate(ref SystemState s)
    {
        var cam = Camera.main; if (!cam) return;
        float3 camPos = cam.transform.position;

        _xf.Update(ref s);

        var em = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        using var ents = _q.ToEntityArray(Allocator.Temp);
        using var xfs = _q.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            float dist = math.distance(xfs[i].Position.xz, camPos.xz);

            // thresholds (tune)
            if (dist < 25f) { SetLOD<LODNear, LODMid, LODFar>(e, em, ref ecb); }
            else if (dist < 60f) { SetLOD<LODMid, LODNear, LODFar>(e, em, ref ecb); }
            else { SetLOD<LODFar, LODNear, LODMid>(e, em, ref ecb); }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void SetLOD<TAdd, TRem1, TRem2>(Entity e, EntityManager em, ref EntityCommandBuffer ecb)
        where TAdd : unmanaged, IComponentData
        where TRem1 : unmanaged, IComponentData
        where TRem2 : unmanaged, IComponentData
    {
        if (!em.HasComponent<TAdd>(e)) ecb.AddComponent<TAdd>(e);
        if (em.HasComponent<TRem1>(e)) ecb.RemoveComponent<TRem1>(e);
        if (em.HasComponent<TRem2>(e)) ecb.RemoveComponent<TRem2>(e);
    }
}
