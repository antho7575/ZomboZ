using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ===== Types (delete if you already define identical ones elsewhere) =====

// Tag on spawned zombies so we know which grid cell they belong to (IComponentData, not shared)
public struct CellCoord : IComponentData { public int2 Value; }


// ===== Jobified incremental streaming system (O(1) idle, O(R) per cell step) =====

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ZombieStreamingSystem : ISystem
{
    // Iterate ONLY currently-loaded zombies (bounded by your window)
    private EntityQuery _loadedZombiesQ;

    public void OnCreate(ref SystemState s)
    {
        _loadedZombiesQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<CellCoord, LocalTransform>()
            .Build(ref s);

        s.RequireForUpdate<ZombieStreamConfig>();
        s.RequireForUpdate<ZombieStreamState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        // Center from camera (swap to FocusPoint if needed)
        if (!SystemAPI.TryGetSingleton<CameraPositionSingleton>(out var cam)) return;
        float3 centerWS = cam.Value;

        var cfgRW = SystemAPI.GetSingletonRW<ZombieStreamConfig>();
        var stRW = SystemAPI.GetSingletonRW<ZombieStreamState>();

        if (!cfgRW.ValueRO.Index.IsCreated || cfgRW.ValueRO.Prefab == Entity.Null) return;
        ref var idx = ref cfgRW.ValueRO.Index.Value;

        int R = math.max(1, cfgRW.ValueRO.LoadRadiusCells);
        int2 curr = WorldToCell(centerWS.xz, idx.cellSize);
        int2 prev = stRW.ValueRO.LastCenterCell;

        // ---------- First time: load full window (O(R^2)) ----------
        if (prev.x == int.MinValue)
        {
            var initCellList = new NativeList<int2>(Allocator.TempJob);
            for (int dz = -R; dz <= R; dz++)
                for (int dx = -R; dx <= R; dx++)
                {
                    int2 c = new int2(curr.x + dx, curr.y + dz);
                    if (InBounds(c, idx.minCoord, idx.size)) initCellList.Add(c);
                }

            BuildLoadArrays(initCellList.AsArray(), cfgRW.ValueRO.Index,
                            out var initCells, out var initStarts, out var initLens);

            var ecbInit = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                                   .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

            var initLoadJob = new LoadCellsJob
            {
                idxRef = cfgRW.ValueRO.Index,
                prefab = cfgRW.ValueRO.Prefab,
                cells = initCells,
                starts = initStarts,
                lens = initLens,
                ecb = ecbInit
            }.Schedule(initCells.Length, 1, s.Dependency); // IJobParallelFor => Schedule

            initCellList.Dispose(initLoadJob);
            initCells.Dispose(initLoadJob);
            initStarts.Dispose(initLoadJob);
            initLens.Dispose(initLoadJob);

            s.Dependency = initLoadJob;
            stRW.ValueRW = new ZombieStreamState { LastCenterCell = curr };
            return;
        }

        // ---------- No cell change -> O(1) skip ----------
        int2 d = curr - prev;
        if (d.x == 0 && d.y == 0) return;

        // ---------- Build incremental O(R) rings ----------
        var toUnload = new NativeList<int2>(Allocator.TempJob);
        var toLoad = new NativeList<int2>(Allocator.TempJob);

        int sx = math.sign(d.x);
        int sy = math.sign(d.y);
        int stepsX = math.abs(d.x);
        int stepsY = math.abs(d.y);

        // X steps
        for (int step = 0; step < stepsX; step++)
        {
            int2 oldC = prev;
            int2 newC = new int2(prev.x + sx, prev.y);

            int xOut = (sx > 0) ? oldC.x - R : oldC.x + R;
            for (int dz = -R; dz <= R; dz++)
            {
                int2 cOut = new int2(xOut, oldC.y + dz);
                if (InBounds(cOut, idx.minCoord, idx.size)) toUnload.Add(cOut);
            }

            int xIn = (sx > 0) ? newC.x + R : newC.x - R;
            for (int dz = -R; dz <= R; dz++)
            {
                int2 cIn = new int2(xIn, newC.y + dz);
                if (InBounds(cIn, idx.minCoord, idx.size)) toLoad.Add(cIn);
            }

            prev = newC;
        }

        // Y steps
        for (int step = 0; step < stepsY; step++)
        {
            int2 oldC = prev;
            int2 newC = new int2(prev.x, prev.y + sy);

            int yOut = (sy > 0) ? oldC.y - R : oldC.y + R;
            for (int dx = -R; dx <= R; dx++)
            {
                int2 cOut = new int2(oldC.x + dx, yOut);
                if (InBounds(cOut, idx.minCoord, idx.size)) toUnload.Add(cOut);
            }

            int yIn = (sy > 0) ? newC.y + R : newC.y - R;
            for (int dx = -R; dx <= R; dx++)
            {
                int2 cIn = new int2(newC.x + dx, newC.y + sy);
                if (InBounds(cIn, idx.minCoord, idx.size)) toLoad.Add(cIn);
            }

            prev = newC;
        }

        // ---------- JOB: Unload (scan ONLY currently-loaded zombies) ----------
        var unloadSet = new NativeParallelHashSet<int2>(toUnload.Length * 2, Allocator.TempJob);
        for (int i = 0; i < toUnload.Length; i++) unloadSet.Add(toUnload[i]);

        var ecbUnload = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                                 .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

        var unloadJob = new UnloadCellsJob
        {
            unloadCells = unloadSet,
            ecb = ecbUnload
        }.ScheduleParallel(s.Dependency);

        unloadSet.Dispose(unloadJob);

        // ---------- JOB: Load ----------
        BuildLoadArrays(toLoad.AsArray(), cfgRW.ValueRO.Index,
                        out var stepCells, out var stepStarts, out var stepLens);

        var ecbLoad = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

        var stepLoadJob = new LoadCellsJob
        {
            idxRef = cfgRW.ValueRO.Index,
            prefab = cfgRW.ValueRO.Prefab,
            cells = stepCells,
            starts = stepStarts,
            lens = stepLens,
            ecb = ecbLoad
        }.Schedule(stepCells.Length, 1, unloadJob); // IJobParallelFor => Schedule

        toUnload.Dispose(stepLoadJob);
        toLoad.Dispose(stepLoadJob);
        stepCells.Dispose(stepLoadJob);
        stepStarts.Dispose(stepLoadJob);
        stepLens.Dispose(stepLoadJob);

        s.Dependency = stepLoadJob;

        // Update last center cell
        stRW.ValueRW = new ZombieStreamState { LastCenterCell = prev };
    }

    // ------------------------ Jobs ------------------------

    [BurstCompile]
    [WithAll(typeof(StreamingManaged), typeof(CellCoord), typeof(LocalTransform))]  // <= filter the query
    public partial struct UnloadCellsJob : IJobEntity     // note: partial is required
    {
        [ReadOnly] public NativeParallelHashSet<int2> unloadCells;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute([ChunkIndexInQuery] int ciq, Entity e, in CellCoord cc)
        {
            if (unloadCells.Contains(cc.Value))
                ecb.DestroyEntity(ciq, e);
        }
    }


    [BurstCompile]
    public struct LoadCellsJob : IJobParallelFor
    {
        public BlobAssetReference<ZombieIndexBlob> idxRef;
        public Entity prefab;

        [ReadOnly] public NativeArray<int2> cells;  // one per job index
        [ReadOnly] public NativeArray<int> starts; // per cell
        [ReadOnly] public NativeArray<int> lens;   // per cell

        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(int i)
        {
            ref var idx = ref idxRef.Value;
            int2 c = cells[i];
            int beg = starts[i];
            int len = lens[i];

            for (int k = 0; k < len; k++)
            {
                var z = ecb.Instantiate(i, prefab);
                float3 p = idx.positions[beg + k];

                ecb.SetComponent(i, z, LocalTransform.FromPositionRotationScale(p, quaternion.identity, 1f));
                ecb.AddComponent<ZombieTag>(i, z);
                ecb.AddComponent(i, z, new Velocity { Value = float3.zero });
                ecb.AddComponent(i, z, new MoveSpeed { Value = 3.5f });
                ecb.AddComponent(i, z, new DesiredVelocity { Value = float3.zero });
                ecb.AddComponent(i, z, new ZombieBlackboard { TimeSinceSeenPlayer = 999f, LastKnownPlayerPos = p, Hunger = 0f });
                ecb.AddComponent<WanderTag>(i, z);
                ecb.AddComponent<WanderState>(i, z);
                
                // Mark cell (regular IComponentData so ECB.AddComponent<T> works)
                ecb.AddComponent(i, z, new CellCoord { Value = c });
                ecb.AddComponent<StreamingManaged>(i, z);
            }
        }
    }

    // ------------------------ Helpers ------------------------

    static int2 WorldToCell(float2 xz, float cell)
        => new int2((int)math.floor(xz.x / cell), (int)math.floor(xz.y / cell));

    static bool InBounds(int2 c, int2 min, int2 size)
        => (uint)(c.x - min.x) < (uint)size.x && (uint)(c.y - min.y) < (uint)size.y;

    static int CellIndex(int2 c, int2 min, int2 size)
        => (c.x - min.x) + (c.y - min.y) * size.x;

    static void BuildLoadArrays(
        NativeArray<int2> cellsIn,
        BlobAssetReference<ZombieIndexBlob> idxRef,
        out NativeArray<int2> outCells,
        out NativeArray<int> outStarts,
        out NativeArray<int> outLens)
    {
        ref var idx = ref idxRef.Value;

        outCells = new NativeArray<int2>(cellsIn.Length, Allocator.TempJob);
        outStarts = new NativeArray<int>(cellsIn.Length, Allocator.TempJob);
        outLens = new NativeArray<int>(cellsIn.Length, Allocator.TempJob);

        for (int i = 0; i < cellsIn.Length; i++)
        {
            int2 c = cellsIn[i];
            int flat = CellIndex(c, idx.minCoord, idx.size);

            outCells[i] = c;
            outStarts[i] = idx.cellStart[flat];
            outLens[i] = idx.cellLen[flat];
        }
    }
}
