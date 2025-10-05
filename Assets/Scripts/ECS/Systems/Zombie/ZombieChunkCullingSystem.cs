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

    public void OnCreate(ref SystemState s)
    {
        _q = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform, ZombieActive>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)  // ← important
            .Build(ref s);

        _xfRO = s.GetComponentTypeHandle<LocalTransform>(true);
        _activeRW = s.GetComponentTypeHandle<ZombieActive>(false);

        // ensure camera singleton exists
        var em = s.EntityManager;
        if (!em.CreateEntityQuery(ComponentType.ReadOnly<CameraPositionSingleton>())
              .TryGetSingletonEntity<CameraPositionSingleton>(out _))
        {
            var e = em.CreateEntity(typeof(CameraPositionSingleton));
            em.SetComponentData(e, new CameraPositionSingleton { Value = float3.zero });
        }
        _entityType = s.GetEntityTypeHandle();
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
        _entityType = s.GetEntityTypeHandle();

        var job = new CullingJob
        {
            camPos = cam,
            nearSqr = nearSqr,
            farSqr = farSqr,
            xfRO = _xfRO,
            activeHandle = _activeRW,
            phase = _phase,
            buckets = Buckets,
            entityType = _entityType,
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
        public ComponentTypeHandle<ZombieActive> activeHandle;

        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public EntityTypeHandle entityType;

        // Entities 1.x signature
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if ((unfilteredChunkIndex % buckets) != phase) return;

            var xforms = chunk.GetNativeArray(ref xfRO);
            int n = xforms.Length;

            var ents = chunk.GetNativeArray(entityType); // for ECB

            for (int i = 0; i < n; i++)
            {
                float3 p = xforms[i].Position;
                float dx = p.x - camPos.x;
                float dz = p.z - camPos.z;
                float d2 = dx * dx + dz * dz;
                if (d2 <= nearSqr) chunk.SetComponentEnabled(ref activeHandle, i, true);
                else if (d2 >= farSqr) chunk.SetComponentEnabled(ref activeHandle, i, false);
                // else keep current state (hysteresis band)

                if (d2 <= nearSqr)
                {
                    // AI ON (ZombieActive is enableable)
                    chunk.SetComponentEnabled(ref activeHandle, i, true);

                    // Render ON (remove DisableRendering)
                    ecb.RemoveComponent<DisableRendering>(unfilteredChunkIndex, ents[i]);
                }
                else if (d2 >= farSqr)
                {
                    // AI OFF
                    chunk.SetComponentEnabled(ref activeHandle, i, false);

                    // Render OFF (add DisableRendering)
                    ecb.AddComponent<DisableRendering>(unfilteredChunkIndex, ents[i]);
                }
            }
        }
    }
}





//using Unity.Burst;
//using Unity.Burst.Intrinsics;                 // v128
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using Unity.Rendering;                        // ChunkWorldRenderBounds, DisableRendering


//[UpdateInGroup(typeof(SimulationSystemGroup))]
////[UpdateAfter(typeof(CameraPosUploader))]   // FocusPoint updated first
//[UpdateBefore(typeof(ZombieWanderSystem))]           // gate AI before it runs
//public partial struct ZombieChunkRenderCullingSystem : ISystem
//{
//    const float Near = 34f, Far = 40f;   // hysteresis
//    const int Buckets = 4;             // process 1/N chunks per frame
//    byte _phase;

//    EntityQuery _q;

//    ComponentTypeHandle<ZombieActive> _activeRW;
//    ComponentTypeHandle<ChunkWorldRenderBounds> _cwrbRO;
//    ComponentTypeHandle<DisableRendering> _disableTagRO; // to check chunk state
//    EntityTypeHandle _entityType;

//    public void OnCreate(ref SystemState s)
//    {
//       // s.RequireForUpdate<CameraPositionSingleton>();
//        s.RequireForUpdate<ZombieTag>();
//        s.RequireForUpdate<ChunkWorldRenderBounds>(); // fast chunk AABB path

//        // Include disabled so we still get chunks when all ZombieActive are off
//        _q = new EntityQueryBuilder(Allocator.Temp)
//            .WithAll<ZombieTag, LocalTransform, ZombieActive>()
//            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
//            .Build(ref s);

//        _activeRW = s.GetComponentTypeHandle<ZombieActive>(false);
//        _cwrbRO = s.GetComponentTypeHandle<ChunkWorldRenderBounds>(true);
//        _disableTagRO = s.GetComponentTypeHandle<DisableRendering>(true);
//        _entityType = s.GetEntityTypeHandle();
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState s)
//    {
//        float3 center = 0;// SystemAPI.GetSingleton<CameraPositionSingleton>().Value;
//        float nearSqr = Near * Near, farSqr = Far * Far;

//        // refresh handles each frame (per docs)
//        _activeRW = s.GetComponentTypeHandle<ZombieActive>(false);
//        _cwrbRO = s.GetComponentTypeHandle<ChunkWorldRenderBounds>(true);
//        _disableTagRO = s.GetComponentTypeHandle<DisableRendering>(true);
//        _entityType = s.GetEntityTypeHandle();

//        var ecb = SystemAPI
//            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//            .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

//        var job = new ChunkCullJob
//        {
//            center = center,
//            nearSqr = nearSqr,
//            farSqr = farSqr,
//            activeHandle = _activeRW,
//            chunkBoundsRO = _cwrbRO,
//            disableTagRO = _disableTagRO,
//            entityType = _entityType,
//            ecb = ecb,
//            phase = _phase,
//            buckets = Buckets
//        };

//        s.Dependency = job.ScheduleParallel(_q, s.Dependency);
//        _phase = (byte)((_phase + 1) % Buckets);
//    }

//    [BurstCompile]
//    struct ChunkCullJob : IJobChunk
//    {
//        public float3 center;
//        public float nearSqr, farSqr;
//        public int phase, buckets;

//        public ComponentTypeHandle<ZombieActive> activeHandle;
//        [ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> chunkBoundsRO;
//        [ReadOnly] public ComponentTypeHandle<DisableRendering> disableTagRO;
//        [ReadOnly] public EntityTypeHandle entityType;

//        public EntityCommandBuffer.ParallelWriter ecb;

//        // Entities 1.x signature
//        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//        {
//            if ((unfilteredChunkIndex % buckets) != phase) return;
//            if (!chunk.Has(ref chunkBoundsRO)) return; // requires Entities Graphics

//            var aabb = chunk.GetChunkComponentData(ref chunkBoundsRO).Value;
//            float d2ch = DistanceSqToAABB(center, aabb);

//            int count = chunk.Count;

//            // --- CPU gate: toggle ZombieActive for this chunk (tight loop over the chunk only)
//            if (d2ch >= farSqr)
//            {
//                for (int i = 0; i < count; i++)
//                    chunk.SetComponentEnabled(ref activeHandle, i, false);
//            }
//            else if (d2ch <= nearSqr)
//            {
//                for (int i = 0; i < count; i++)
//                    chunk.SetComponentEnabled(ref activeHandle, i, true);
//            }
//            else
//            {
//                // in hysteresis band: keep current AI state
//            }

//            // --- Render toggle only when needed (avoid spamming structural ops)
//            bool chunkHasDisable = chunk.Has(ref disableTagRO);

//            if (d2ch >= farSqr)
//            {
//                if (!chunkHasDisable)
//                {
//                    var ents = chunk.GetNativeArray(entityType);
//                    for (int i = 0; i < count; i++)
//                        ecb.AddComponent<DisableRendering>(unfilteredChunkIndex, ents[i]);
//                }
//            }
//            else if (d2ch <= nearSqr)
//            {
//                if (chunkHasDisable)
//                {
//                    var ents = chunk.GetNativeArray(entityType);
//                    for (int i = 0; i < count; i++)
//                        ecb.RemoveComponent<DisableRendering>(unfilteredChunkIndex, ents[i]);
//                }
//            }
//        }

//        static float DistanceSqToAABB(float3 p, Unity.Mathematics.AABB a)
//        {
//            float3 min = a.Center - a.Extents;
//            float3 max = a.Center + a.Extents;
//            float3 q = math.clamp(p, min, max);
//            return math.lengthsq(p - q);
//        }
//    }
//}
