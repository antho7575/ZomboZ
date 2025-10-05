//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial struct GridCellAssignmentSystem : ISystem
//{
//    const float CellSize = 16f;
//    const int Buckets = 4;   // process 1/N per frame
//    byte _phase;

//    EntityQuery _q;

//    [BurstCompile]
//    public void OnCreate(ref SystemState s)
//    {
//        // We assume your spawner adds CurrentCell at creation.
//        s.RequireForUpdate<ZombieTag>();
//        s.RequireForUpdate<LocalTransform>();
//        s.RequireForUpdate<CurrentCell>();

//        _q = new EntityQueryBuilder(Allocator.Temp)
//            .WithAll<ZombieTag, LocalTransform, CurrentCell>()
//            .Build(ref s);
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState s)
//    {
//        // Collect only the entities that CHANGED cell in a parallel job.
//        var changes = new NativeQueue<CellChange>(Allocator.TempJob);

//        s.Dependency = new ComputeCellChangesJob
//        {
//            invCellSize = 1f / CellSize,
//            phase = _phase,
//            buckets = Buckets,
//            outQueue = changes.AsParallelWriter()
//        }.ScheduleParallel(_q, s.Dependency);

//        // We must complete before we can read 'changes' on the main thread.
//        s.Dependency.Complete();

//        // Apply minimal structural changes only for entities that actually moved.
//        var em = s.EntityManager;
//        while (changes.TryDequeue(out var change))
//        {
//            // Skip if already in the same shared cell (avoid pointless structural work)
//            if (em.HasComponent<GridCell>(change.Entity))
//            {
//                var old = em.GetSharedComponentManaged<GridCell>(change.Entity);
//                if (old.Coord.x == change.Coord.x && old.Coord.y == change.Coord.y)
//                    continue;
//            }
//            em.SetSharedComponentManaged(change.Entity, new GridCell { Coord = change.Coord });
//        }

//        changes.Dispose();

//        _phase = (byte)((_phase + 1) % Buckets);
//    }

//    // ---------------------------------------------------------

//    [BurstCompile]
//    partial struct ComputeCellChangesJob : IJobEntity
//    {
//        public float invCellSize;
//        public int phase;
//        public int buckets;

//        public NativeQueue<CellChange>.ParallelWriter outQueue;

//        // Throttle with [EntityIndexInQuery] to spread work across frames
//        void Execute(Entity e,
//                     ref CurrentCell cc,
//                     in LocalTransform xf,
//                     [EntityIndexInQuery] int index)
//        {
//            if ((index % buckets) != phase) return;
//            if ((index % buckets) != phase) return;

//            int2 newC = new int2(
//                (int)math.floor(xf.Position.x * invCellSize),
//                (int)math.floor(xf.Position.z * invCellSize));

//            if (newC.x != cc.Coord.x || newC.y != cc.Coord.y)
//            {
//                cc.Coord = newC; // cheap IComponentData write (Burstable)
//                outQueue.Enqueue(new CellChange { Entity = e, Coord = newC });
//            }
//        }
//    }

//    struct CellChange
//    {
//        public Entity Entity;
//        public int2 Coord;
//    }
//}
