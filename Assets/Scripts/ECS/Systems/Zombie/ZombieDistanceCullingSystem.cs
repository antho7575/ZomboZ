using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

//[DisableAutoCreation]
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieSpawnSystem))]
public partial struct ZombieDistanceCullingSystem : ISystem
{
    private EntityQuery _zombieQuery;

    // Lookups we need
    private ComponentLookup<LocalTransform> _xfRO;
    private ComponentLookup<MaterialMeshInfo> _mmiRO;
    private BufferLookup<LinkedEntityGroup> _legRO;

    private float _accum;

    public void OnCreate(ref SystemState s)
    {
        _zombieQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform>()
            .Build(ref s);

        _xfRO = s.GetComponentLookup<LocalTransform>(true);
        _mmiRO = s.GetComponentLookup<MaterialMeshInfo>(true);
        _legRO = s.GetBufferLookup<LinkedEntityGroup>(true);
    }

    public void OnUpdate(ref SystemState s)
    {
        var camObj = Camera.main;
        if (!camObj) return;
        float3 cam = (float3)camObj.transform.position;

        const float maxVisibleRadius = 40f;
        float maxSq = maxVisibleRadius * maxVisibleRadius;

        var em = s.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        _xfRO.Update(ref s);
        _mmiRO.Update(ref s);
        _legRO.Update(ref s);

        int total = 0, culled = 0;

        using var ents = _zombieQuery.ToEntityArray(Allocator.Temp);
        using var xforms = _zombieQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        //for (int i = 0; i < ents.Length; i++)
        //{
        //    var root = ents[i];
        //    var xf = xforms[i];
        //    total++;

        //    // choose the entity that actually renders (has MaterialMeshInfo)
        //    Entity renderE = root;
        //    if (!_mmiRO.HasComponent(renderE))
        //    {
        //        if (_legRO.HasBuffer(root))
        //        {
        //            var buf = _legRO[root];
        //            for (int j = 0; j < buf.Length; j++)
        //            {
        //                var child = buf[j].Value;
        //                if (_mmiRO.HasComponent(child)) { renderE = child; break; }
        //            }
        //        }
        //    }

        //    float3 d = xf.Position - cam; d.y = 0;
        //    bool far = math.lengthsq(d) > maxSq;

        //    bool hidden = em.HasComponent<DisableRendering>(renderE);
        //    if (far && !hidden)
        //    {
        //        ecb.AddComponent<DisableRendering>(renderE); ecb.AddComponent<WanderState>(renderE); culled++;
        //    }
        //    else if (!far && hidden)
        //    {
        //        ecb.DestroyEntity(root); // force respawn via spawner   
        //    }
        //    else if (hidden) { culled++; }
        //}

        ecb.Playback(em);
        ecb.Dispose();

        _accum += SystemAPI.Time.DeltaTime;
        if (_accum >= 1f)
        {
            Debug.Log($"[Culling] total={total} culled={culled} radius={maxVisibleRadius}");
            _accum = 0f;
        }
    }
}
