using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering; // URPMaterialPropertyBaseColor

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(WorldGenSystem))]
public partial struct MapDebugTilesSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<MapGridSingleton>();
        s.RequireForUpdate<MapDebugTilesConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var cfg = SystemAPI.GetSingleton<MapDebugTilesConfig>();
        if (cfg.Prefab == Entity.Null) return;

        ref var grid = ref SystemAPI.GetSingleton<MapGridSingleton>().Grid.Value;
        int2 size = grid.size;
        float cs = grid.cellSize;

        int stride = math.max(1, cfg.Stride);
        int nx = (size.x + stride - 1) / stride;
        int ny = (size.y + stride - 1) / stride;
        int total = nx * ny;
        if (cfg.MaxInstances > 0) total = math.min(total, cfg.MaxInstances);

        // ✅ SAFE to query PREFAB with EntityManager (it's a realized entity)
        bool prefabHasColor = s.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(cfg.Prefab);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        int made = 0;
        for (int yy = 0; yy < size.y && made < total; yy += stride)
        {
            for (int xx = 0; xx < size.x && made < total; xx += stride)
            {
                int idx = xx + yy * size.x;

                // Create deferred instance
                Entity e = ecb.Instantiate(cfg.Prefab);

                // Transform
                float h01 = grid.height[idx];
                float y = (cfg.UseHeight != 0) ? h01 * cfg.HeightScale : 0f;
                float3 pos = new float3((xx + 0.5f) * cs, y, (yy + 0.5f) * cs);
                float scale = cs * 0.98f;
                ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));

                // Color (only use ECB ops on 'e', never EntityManager)
                if (cfg.ColorByBiome != 0)
                {
                    float4 col = BiomeColor((BiomeId)grid.biome[idx]);
                    if (prefabHasColor)
                        ecb.SetComponent(e, new URPMaterialPropertyBaseColor { Value = col });
                    else
                        ecb.AddComponent(e, new URPMaterialPropertyBaseColor { Value = col });
                }

                made++;
            }
        }

        // Realize all deferred entities
        ecb.Playback(s.EntityManager);
        ecb.Dispose();

        // Stop running after one spawn (optional gate)
        var q = s.EntityManager.CreateEntityQuery(typeof(MapDebugTilesConfig), typeof(MapDebugTilesSpawned));
        if (!q.IsEmpty)
            s.EntityManager.RemoveComponent<MapDebugTilesSpawned>(q);
        q.Dispose();
    }


    static float4 BiomeColor(BiomeId b)
    {
        // tweak to taste
        return b switch
        {
            BiomeId.Plains => new float4(0.35f, 0.75f, 0.35f, 1f),
            BiomeId.Forest => new float4(0.10f, 0.45f, 0.12f, 1f),
            BiomeId.Desert => new float4(0.85f, 0.75f, 0.35f, 1f),
            BiomeId.Snow => new float4(0.90f, 0.95f, 1.00f, 1f),
            BiomeId.Water => new float4(0.20f, 0.40f, 0.95f, 1f),
            _ => new float4(0.6f, 0.6f, 0.6f, 1f),
        };
    }
}
