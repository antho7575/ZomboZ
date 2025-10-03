using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct DebugDrawFlowFieldSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        var q = SystemAPI.QueryBuilder().WithAll<GridConfig, CellDir>().Build();
        using var grids = q.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (grids.Length == 0) return;

        var e = grids[0];
        var cfg = em.GetComponentData<GridConfig>(e);
        var dirsB = em.GetBuffer<CellDir>(e);
        if (dirsB.Length == 0) return;

        var size = cfg.Size;
        var dirs = dirsB.AsNativeArray();

        int step = math.max(1, math.max(size.x, size.y) / 32); // draw fewer arrows on big grids
        for (int y = 0; y < size.y; y += step)
            for (int x = 0; x < size.x; x += step)
            {
                int i = y * size.x + x;
                var d = dirs[i].Value;
                var start = new Vector3(cfg.OriginWS.x + (x + 0.5f) * cfg.CellSize, 0.1f, cfg.OriginWS.y + (y + 0.5f) * cfg.CellSize);
                var end = start + new Vector3(d.x, 0f, d.y) * (cfg.CellSize * 0.8f);
                Debug.DrawLine(start, end, Color.yellow);
            }
    }
}
