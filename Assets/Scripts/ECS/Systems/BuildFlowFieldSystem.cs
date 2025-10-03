using Unity.Entities;
using Unity.Mathematics;

public partial struct BuildFlowFieldSystem : ISystem
{
    public void OnCreate(ref SystemState s) => s.RequireForUpdate<FlowFieldTag>();

    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;

        var q = SystemAPI.QueryBuilder()
            .WithAll<GridConfig, CellCost, CellIntegration, CellDir>()
            .Build();

        using var entities = q.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int eIdx = 0; eIdx < entities.Length; eIdx++)
        {
            var e = entities[eIdx];
            var cfg = em.GetComponentData<GridConfig>(e);
            var costs = em.GetBuffer<CellCost>(e);
            var integ = em.GetBuffer<CellIntegration>(e);
            var dirs = em.GetBuffer<CellDir>(e);

            var size = cfg.Size;
            int count = size.x * size.y;

            if (costs.Length != count) { costs.ResizeUninitialized(count); for (int i = 0; i < count; i++) costs[i] = new CellCost { Value = 1 }; }
            if (integ.Length != count) integ.ResizeUninitialized(count);
            if (dirs.Length != count) dirs.ResizeUninitialized(count);

            var target = new int2(size.x / 2, size.y / 2);
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    int idx = y * size.x + x;
                    var d = target - new int2(x, y);
                    float2 dir = math.lengthsq(d) > 0 ? math.normalize(new float2(d.x, d.y)) : float2.zero;
                    dirs[idx] = new CellDir { Value = dir };
                    integ[idx] = new CellIntegration { Value = (uint)math.lengthsq(d) };
                }
        }
    }
}
