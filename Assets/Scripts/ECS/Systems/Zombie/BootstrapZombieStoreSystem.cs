using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Marker so other systems can RequireForUpdate<ZombieStoreTag>()
public struct ZombieStoreTag : IComponentData { }

// Runtime holder for the store (NOT an ECS component)
public static class ZombieStoreRuntime
{
    public static bool IsCreated;
    public static NativeParallelMultiHashMap<int2, ZombieRecord> BySector;
    public static NativeParallelHashMap<int, ZombieRecord> ById;

    public static void CreateIfNeeded(int bySectorCapacity = 8192, int byIdCapacity = 16384)
    {
        if (IsCreated) return;
        BySector = new NativeParallelMultiHashMap<int2, ZombieRecord>(bySectorCapacity, Allocator.Persistent);
        ById = new NativeParallelHashMap<int, ZombieRecord>(byIdCapacity, Allocator.Persistent);
        IsCreated = true;
    }

    public static void Dispose()
    {
        if (!IsCreated) return;
        if (BySector.IsCreated) BySector.Dispose();
        if (ById.IsCreated) ById.Dispose();
        IsCreated = false;
    }
}


[DisableAutoCreation]
// Bootstrap it once (creates marker + fills the runtime store)
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BootstrapZombieStoreSystem : ISystem
{
    public void OnCreate(ref SystemState s)
    {
        var em = s.EntityManager;

        // Ensure the runtime store exists
        ZombieStoreRuntime.CreateIfNeeded();

        // Ensure marker singleton exists so other systems can RequireForUpdate<ZombieStoreTag>()
        if (!SystemAPI.HasSingleton<ZombieStoreTag>())
        {
            var e = em.CreateEntity();
            em.AddComponent<ZombieStoreTag>(e);

            // Optional: prefill a big area of records (procedural)
            var rnd = Unity.Mathematics.Random.CreateFromIndex(123u);
            int id = 1;
            for (int sx = -20; sx <= 20; sx++)
                for (int sy = -20; sy <= 20; sy++)
                {
                    int count = rnd.NextInt(10, 20); // density per sector
                    for (int i = 0; i < count; i++)
                    {
                        float3 p = new float3(
                            sx * 64 + rnd.NextFloat(-32, 32),
                            0,
                            sy * 64 + rnd.NextFloat(-32, 32));

                        var rec = new ZombieRecord
                        {
                            Id = id++,
                            Sector = new int2(sx, sy),
                            Pos = p,
                            Heading = rnd.NextFloat(0, 6.2831853f),
                            WanderSeed = rnd.state,
                            TimeSinceSeen = 999f
                        };

                        ZombieStoreRuntime.BySector.Add(rec.Sector, rec);
                        ZombieStoreRuntime.ById.TryAdd(rec.Id, rec);
                    }
                }
        }

        // run once at startup
        s.Enabled = false;
    }

    public void OnDestroy(ref SystemState s)
    {
        // Dispose native containers when the World is destroyed
        ZombieStoreRuntime.Dispose();
    }
}
