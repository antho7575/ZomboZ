using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering; // <-- use built-in DisableRendering

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct DebugDrawCulledZombiesSystem : ISystem
{
    private EntityQuery _culledQuery;
    private ComponentLookup<LocalTransform> _xfRO;

    public void OnCreate(ref SystemState s)
    {
        _culledQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieTag, DisableRendering, LocalTransform>() // only culled zombies
            .Build(ref s);

        _xfRO = s.GetComponentLookup<LocalTransform>(isReadOnly: true);
    }

    public void OnUpdate(ref SystemState s)
    {
        _xfRO.Update(ref s);

        using var ents = _culledQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var xf = _xfRO[e];
            var p = xf.Position;
            Debug.DrawLine(p, p + new float3(0, 1f, 0), Color.red, 0f, false);
        }
    }
}
