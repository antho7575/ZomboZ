using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class SimpleZombieStreamer : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ZombieStreamConfig>();
        RequireForUpdate<ZombieIndexRef>();
    }

    protected override void OnUpdate()
    {
        var em = EntityManager;
        var cfg = SystemAPI.GetSingleton<ZombieStreamConfig>();
        var blob = SystemAPI.GetSingleton<ZombieIndexRef>().Blob;
        ref var idx = ref blob.Value;

        // Player position (or origin if none)
        float3 playerPos = float3.zero;
        foreach (var lt in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        { playerPos = lt.ValueRO.Position; break; }

        int2 pCell = new((int)math.floor(playerPos.x / cfg.CellSize),
                         (int)math.floor(playerPos.z / cfg.CellSize));

        int r = cfg.VisibleRadiusCells;
        var toLoad = new NativeList<int2>(Allocator.Temp);

        // Pick only nearby cells to spawn
        for (int i = 0; i < idx.uniqueCells.Length; i++)
        {
            var c = idx.uniqueCells[i];
            if (math.max(math.abs(c.x - pCell.x), math.abs(c.y - pCell.y)) <= r)
                toLoad.Add(c);
        }

        // Spawn for each visible cell
        foreach (var c in toLoad)
        {
            // find cell slice
            int slice = -1;
            for (int i = 0; i < idx.uniqueCells.Length; i++)
                if (idx.uniqueCells[i].x == c.x && idx.uniqueCells[i].y == c.y) { slice = i; break; }

            if (slice < 0) continue;
            int start = idx.cellStart[slice];
            int len = idx.cellLen[slice];

            for (int k = 0; k < len; k++)
            {
                float3 p = idx.positions[start + k];
                var e = em.Instantiate(cfg.Prefab);
                em.SetComponentData(e, LocalTransform.FromPositionRotationScale(p, quaternion.identity, 1f));
            }
        }

        toLoad.Dispose();

        Enabled = false; // run once for debugging
    }
}
