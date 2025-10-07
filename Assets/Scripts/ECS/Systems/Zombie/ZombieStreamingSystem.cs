using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering; // DisableRendering

// ===== Tags (delete if you already define these elsewhere) =====
public struct CellCoord : IComponentData { public int2 Value; }           // which grid cell an entity belongs to


// ===== Jobified incremental streaming system (O(1) idle, O(R) per cell step) =====
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ZombieStreamingSystem : ISystem
{
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<ZombieStreamConfig>();
        s.RequireForUpdate<ZombieStreamState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        if (!SystemAPI.TryGetSingleton<CameraPositionSingleton>(out var cam)) return;
        float3 centerWS = cam.Value;

        var cfgRW = SystemAPI.GetSingletonRW<ZombieStreamConfig>();
        var stRW = SystemAPI.GetSingletonRW<ZombieStreamState>();
        var cfg = cfgRW.ValueRO;

        if (!cfg.Index.IsCreated || cfg.Prefab == Entity.Null) return;

        ref var idx = ref cfg.Index.Value;

        // --- Dynamic radii (no extra loops) ---
        // Estimate planar speed (m/s)
        float dt = math.max(SystemAPI.Time.DeltaTime, 1e-4f);
        float2 dxy = (centerWS.xz - stRW.ValueRO.LastCenterWS.xz);
        float speed = math.length(dxy) / dt;

        int extra = (cfg.LookaheadSeconds > 0f) ? (int)math.ceil(speed * cfg.LookaheadSeconds / idx.cellSize) : 0;
        int Rload = math.max(1, cfg.LoadRadiusCells + extra);
        int Rvis = math.max(1, Rload - math.max(0, cfg.VisibleMarginCells));

        int2 curr = WorldToCell(centerWS.xz, idx.cellSize);
        int2 prev = stRW.ValueRO.LastCenterCell;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

        // ---------- First time: load full window (O(R^2)) ----------
        if (prev.x == int.MinValue)
        {
            var initCells = new NativeList<int2>(Allocator.TempJob);
            for (int dz = -Rload; dz <= Rload; dz++)
                for (int dx = -Rload; dx <= Rload; dx++)
                {
                    int2 c = new int2(curr.x + dx, curr.y + dz);
                    if (InBounds(c, idx.minCoord, idx.size)) initCells.Add(c);
                }

            BuildLoadArrays(initCells.AsArray(), cfg.Index, out var cells, out var starts, out var lens);

            var loadCellsJob = new LoadCellsJob
            {
                idxRef = cfg.Index,
                prefab = cfg.Prefab,
                cells = cells,
                starts = starts,
                lens = lens,
                centerCellAtLoad = curr,
                visibleRadiusCells = Rvis,
                ecb = ecb
            }.Schedule(cells.Length, 1, s.Dependency);

            initCells.Dispose(loadCellsJob);
            cells.Dispose(loadCellsJob);
            starts.Dispose(loadCellsJob);
            lens.Dispose(loadCellsJob);

            s.Dependency = loadCellsJob;
            stRW.ValueRW = new ZombieStreamState { LastCenterCell = curr, LastCenterWS = centerWS };
            return;
        }

        // ---------- If center cell didn't change → O(1) skip ----------
        int2 dCell = curr - prev;
        if (dCell.x == 0 && dCell.y == 0)
        {
            stRW.ValueRW.LastCenterWS = centerWS;
            return;
        }

        // ---------- Build incremental O(R) rings + promote stripes ----------
        var toUnload = new NativeList<int2>(Allocator.TempJob);
        var toLoad = new NativeList<int2>(Allocator.TempJob);
        var toPromote = new NativeList<int2>(Allocator.TempJob); // cells entering visibility band

        int sx = math.sign(dCell.x);
        int sy = math.sign(dCell.y);
        int stepsX = math.abs(dCell.x);
        int stepsY = math.abs(dCell.y);

        // Move along X in steps
        for (int step = 0; step < stepsX; step++)
        {
            int2 oldC = prev;
            int2 newC = new int2(prev.x + sx, prev.y);

            // Unload leaving column (load window)
            int xOut = (sx > 0) ? oldC.x - Rload : oldC.x + Rload;
            for (int dz = -Rload; dz <= Rload; dz++)
            {
                int2 cOut = new int2(xOut, oldC.y + dz);
                if (InBounds(cOut, idx.minCoord, idx.size)) toUnload.Add(cOut);
            }
            // Load entering column (load window)
            int xIn = (sx > 0) ? newC.x + Rload : newC.x - Rload;
            for (int dz = -Rload; dz <= Rload; dz++)
            {
                int2 cIn = new int2(xIn, newC.y + dz);
                if (InBounds(cIn, idx.minCoord, idx.size)) toLoad.Add(cIn);
            }
            // Promote newly visible column (inner square)
            int xProm = newC.x + sx * Rvis;
            for (int dz = -Rvis; dz <= Rvis; dz++)
            {
                int2 cp = new int2(xProm, newC.y + dz);
                if (InBounds(cp, idx.minCoord, idx.size)) toPromote.Add(cp);
            }

            prev = newC;
        }

        // Move along Y in steps
        for (int step = 0; step < stepsY; step++)
        {
            int2 oldC = prev;
            int2 newC = new int2(prev.x, prev.y + sy);

            // Unload leaving row
            int yOut = (sy > 0) ? oldC.y - Rload : oldC.y + Rload;
            for (int dx = -Rload; dx <= Rload; dx++)
            {
                int2 cOut = new int2(oldC.x + dx, yOut);
                if (InBounds(cOut, idx.minCoord, idx.size)) toUnload.Add(cOut);
            }
            // Load entering row
            int yIn = (sy > 0) ? newC.y + Rload : newC.y - Rload;
            for (int dx = -Rload; dx <= Rload; dx++)
            {
                int2 cIn = new int2(newC.x + dx, yIn);
                if (InBounds(cIn, idx.minCoord, idx.size)) toLoad.Add(cIn);
            }
            // Promote newly visible row
            int yProm = newC.y + sy * Rvis;
            for (int dx = -Rvis; dx <= Rvis; dx++)
            {
                int2 cp = new int2(newC.x + dx, yProm);
                if (InBounds(cp, idx.minCoord, idx.size)) toPromote.Add(cp);
            }

            prev = newC;
        }

        // ---------- JOB: Unload (destroy leaving cells) ----------
        var unloadSet = new NativeParallelHashSet<int2>(toUnload.Length * 2, Allocator.TempJob);
        for (int i = 0; i < toUnload.Length; i++) unloadSet.Add(toUnload[i]);

        var unloadJob = new UnloadCellsJob
        {
            unloadCells = unloadSet,
            ecb = ecb
        }.ScheduleParallel(s.Dependency);

        unloadSet.Dispose(unloadJob);

        // ---------- JOB: Promote (unhide cells entering visibility) ----------
        var promoteSet = new NativeParallelHashSet<int2>(toPromote.Length * 2, Allocator.TempJob);
        for (int i = 0; i < toPromote.Length; i++) promoteSet.Add(toPromote[i]);

        var promoteJob = new PromoteVisibleJob
        {
            promoteCells = promoteSet,
            ecb = ecb
        }.ScheduleParallel(unloadJob);

        promoteSet.Dispose(promoteJob);

        // ---------- JOB: Load (spawn entering load window) ----------
        BuildLoadArrays(toLoad.AsArray(), cfg.Index, out var stepCells, out var stepStarts, out var stepLens);

        var loadJob = new LoadCellsJob
        {
            idxRef = cfg.Index,
            prefab = cfg.Prefab,
            cells = stepCells,
            starts = stepStarts,
            lens = stepLens,
            centerCellAtLoad = curr,
            visibleRadiusCells = Rvis,
            ecb = ecb
        }.Schedule(stepCells.Length, 1, promoteJob);

        toUnload.Dispose(loadJob);
        toLoad.Dispose(loadJob);
        toPromote.Dispose(loadJob);
        stepCells.Dispose(loadJob);
        stepStarts.Dispose(loadJob);
        stepLens.Dispose(loadJob);

        s.Dependency = loadJob;

        // Update state
        stRW.ValueRW = new ZombieStreamState { LastCenterCell = curr, LastCenterWS = centerWS };
    }

    // ------------------------ Jobs ------------------------

    // Destroy any stream-managed entity whose CellCoord is in unloadCells.
    [BurstCompile]
    [WithAll(typeof(StreamingManaged), typeof(CellCoord), typeof(LocalTransform))]
    public partial struct UnloadCellsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashSet<int2> unloadCells;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute([ChunkIndexInQuery] int ciq, Entity e, in CellCoord cc)
        {
            if (unloadCells.Contains(cc.Value))
                ecb.DestroyEntity(ciq, e);
        }
    }

    // Remove DisableRendering for entities whose CellCoord is now in the visible stripe.
    [BurstCompile]
    [WithAll(typeof(StreamingManaged), typeof(CellCoord), typeof(LocalTransform))]
    public partial struct PromoteVisibleJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashSet<int2> promoteCells;
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute([ChunkIndexInQuery] int ciq, Entity e, in CellCoord cc)
        {
            if (promoteCells.Contains(cc.Value))
                ecb.RemoveComponent<DisableRendering>(ciq, e); // unhide
        }
    }

    // Spawn a cell's positions; hide if the cell is outside the visible radius at spawn-time.
    [BurstCompile]
    public partial struct LoadCellsJob : IJobParallelFor
    {
        public BlobAssetReference<ZombieIndexBlob> idxRef;
        public Entity prefab;

        [ReadOnly] public NativeArray<int2> cells;  // one per job index
        [ReadOnly] public NativeArray<int> starts; // per cell
        [ReadOnly] public NativeArray<int> lens;   // per cell

        public int2 centerCellAtLoad;
        public int visibleRadiusCells; // Chebyshev

        public EntityCommandBuffer.ParallelWriter ecb;

        static int Cheby(int2 a, int2 b) => math.max(math.abs(a.x - b.x), math.abs(a.y - b.y));

        public void Execute(int i)
        {
            ref var idx = ref idxRef.Value;
            int2 c = cells[i];
            int beg = starts[i];
            int len = lens[i];

            bool makeVisible = Cheby(c, centerCellAtLoad) <= visibleRadiusCells;

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
                ecb.AddComponent<StreamingManaged>(i, z);
                ecb.AddComponent(i, z, new CellCoord { Value = c });
                ecb.AddComponent<WanderTag>(i, z);
                ecb.AddComponent<WanderState>(i, z);

                if (!makeVisible)
                    ecb.AddComponent<DisableRendering>(i, z); // pre-warmed but hidden
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
