using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Make sure this runs AFTER the flow field is built each frame
[UpdateAfter(typeof(BuildFlowFieldSystem))]
public partial struct ZombieSteeringSystem : ISystem
{
    public void OnCreate(ref SystemState s) => s.RequireForUpdate<FlowFieldTag>();

    public void OnUpdate(ref SystemState s)
    {
        //var em = s.EntityManager;
        //float dt = SystemAPI.Time.DeltaTime;

        //// Find the grid owner
        //var qGrid = SystemAPI.QueryBuilder().WithAll<GridConfig, CellDir>().Build();
        //using var gridEntities = qGrid.ToEntityArray(Unity.Collections.Allocator.Temp);
        //if (gridEntities.Length == 0) return;

        //var gridE = gridEntities[0];
        //var grid = em.GetComponentData<GridConfig>(gridE);

        //// If the buffers aren't ready yet, bail out safely
        //var dirBuf = em.GetBuffer<CellDir>(gridE);
        //if (dirBuf.Length == 0) return; // <-- guard
        //var dirs = dirBuf.AsNativeArray();

        //int cellCount = grid.Size.x * grid.Size.y;
        //if (dirBuf.Length < cellCount) return; // extra guard in case of partial init

        //// Iterate zombies
        //var qZ = SystemAPI.QueryBuilder()
        //    .WithAll<ZombieTag, LocalTransform, MoveSpeed, Velocity>()
        //    .Build();

        //using var zombies = qZ.ToEntityArray(Unity.Collections.Allocator.Temp);
        //for (int i = 0; i < zombies.Length; i++)
        //{
        //    var e = zombies[i];
        //    var xform = em.GetComponentData<LocalTransform>(e);
        //    var speed = em.GetComponentData<MoveSpeed>(e);
        //    var vel = em.GetComponentData<Velocity>(e);

        //    int2 cell = GridUtil.WorldToCell(xform.Position, grid.OriginWS, grid.CellSize);
        //    float2 d2 = float2.zero;

        //    if (GridUtil.InBounds(cell, grid.Size))
        //    {
        //        int idx = cell.y * grid.Size.x + cell.x;
        //        d2 = dirs[idx].Value;
        //    }

        //    float3 desired = new float3(d2.x, 0, d2.y) * speed.Value;
        //    vel.Value = math.lerp(vel.Value, desired, 0.25f);
        //    xform = xform.WithPosition(xform.Position + vel.Value * dt);

        //    em.SetComponentData(e, xform);
        //    em.SetComponentData(e, vel);
       // }
    }
}
