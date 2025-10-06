using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ZombieChunkCullingSystem : ISystem
{
    const float Near = 34f, Far = 40f;
    const int Buckets = 4;
    byte _phase;

    EntityQuery _q;
    ComponentTypeHandle<LocalTransform> _xfRO;
    ComponentTypeHandle<ZombieActive> _activeRW;
    EntityTypeHandle _entityType;

    // type handle:
    ComponentTypeHandle<PrimaryRenderEntity> _renderRefRO;
    public void OnCreate(ref SystemState s)
    {
        _q = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform, ZombieActive>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)  // ← important
            .Build(ref s);

        _xfRO = s.GetComponentTypeHandle<LocalTransform>(true);
        _activeRW = s.GetComponentTypeHandle<ZombieActive>(false);
        _renderRefRO = s.GetComponentTypeHandle<PrimaryRenderEntity>(true);

        // ensure camera singleton exists
        var em = s.EntityManager;
        if (!em.CreateEntityQuery(ComponentType.ReadOnly<CameraPositionSingleton>())
              .TryGetSingletonEntity<CameraPositionSingleton>(out _))
        {
            var e = em.CreateEntity(typeof(CameraPositionSingleton));
            em.SetComponentData(e, new CameraPositionSingleton { Value = float3.zero });
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        float3 cam = SystemAPI.GetSingleton<CameraPositionSingleton>().Value;
        float nearSqr = Near * Near, farSqr = Far * Far;

        _xfRO = s.GetComponentTypeHandle<LocalTransform>(true);
        _activeRW = s.GetComponentTypeHandle<ZombieActive>(false);

        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

        // refresh each frame:
        _renderRefRO = s.GetComponentTypeHandle<PrimaryRenderEntity>(true);

        var job = new CullingJob
        {
            camPos = cam,
            nearSqr = nearSqr,
            farSqr = farSqr,
            xfRO = _xfRO,
            activeHandle = _activeRW,
            phase = _phase,
            buckets = Buckets,
            renderRefRO = _renderRefRO,
            ecb = ecb
        };

        s.Dependency = job.ScheduleParallel(_q, s.Dependency);
        _phase = (byte)((_phase + 1) % Buckets);
    }

    [BurstCompile]
    struct CullingJob : IJobChunk
    {
        public float3 camPos;
        public float nearSqr, farSqr;
        public int phase, buckets;

        [ReadOnly] public ComponentTypeHandle<LocalTransform> xfRO;
        [ReadOnly] public ComponentTypeHandle<PrimaryRenderEntity> renderRefRO;
        public ComponentTypeHandle<ZombieActive> activeHandle;

        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 mask)
        {
            if ((unfilteredChunkIndex % buckets) != phase) return;

            var xforms = chunk.GetNativeArray(ref xfRO);
            var renders = chunk.GetNativeArray(ref renderRefRO);
            int n = xforms.Length;

            for (int i = 0; i < n; i++)
            {
                float3 p = xforms[i].Position;
                float dx = p.x - camPos.x;
                float dz = p.z - camPos.z;
                float d2 = dx * dx + dz * dz;

                // O(1) per entity: flip AI gate
                if (d2 <= nearSqr) chunk.SetComponentEnabled(ref activeHandle, i, true);
                else if (d2 >= farSqr) chunk.SetComponentEnabled(ref activeHandle, i, false);

                // O(1) per entity: toggle render on the *correct* entity
                var renderE = renders[i].Value;
                if (renderE == Entity.Null) continue; // safety

                if (d2 <= nearSqr)
                    ecb.RemoveComponent<DisableRendering>(unfilteredChunkIndex, renderE);
                else if (d2 >= farSqr)
                    ecb.AddComponent<DisableRendering>(unfilteredChunkIndex, renderE);
            }
        }
    }

}