using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public struct MapDebugTilesConfig : IComponentData
{
    public Entity Prefab;       // renderable prefab (converted)
    public int Stride;        // sample every N cells (8..32 good)
    public float HeightScale;   // vertical exaggeration
    public byte UseHeight;     // 1 = raise tiles by height
    public byte ColorByBiome;  // 1 = color per biome
    public int MaxInstances;  // safety cap
}
public struct MapDebugTilesSpawned : IComponentData { } // tag so we run once

public class MapDebugTilesAuthoring : MonoBehaviour
{
    [Header("Prefab (MeshRenderer in SubScene)")]
    public GameObject TilePrefab;

    [Header("Sampling / Visuals")]
    [Range(1, 128)] public int stride = 16;
    public float heightScale = 40f;
    public bool useHeight = true;
    public bool colorByBiome = true;
    public int maxInstances = 50000;

    class Baker : Baker<MapDebugTilesAuthoring>
    {
        public override void Bake(MapDebugTilesAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);

            AddComponent(e, new MapDebugTilesConfig
            {
                Prefab = GetEntity(a.TilePrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                Stride = math.max(1, a.stride),
                HeightScale = a.heightScale,
                UseHeight = (byte)(a.useHeight ? 1 : 0),
                ColorByBiome = (byte)(a.colorByBiome ? 1 : 0),
                MaxInstances = math.max(0, a.maxInstances)
            });

            // Optional run-once gate:
            AddComponent<MapDebugTilesSpawned>(e);

            // (Optional) ensure prefab already has the color component so we can SetComponent without checks:
            var prefabE = GetEntity(a.TilePrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            AddComponent(prefabE, new URPMaterialPropertyBaseColor { Value = new float4(1, 1, 1, 1) });
        }
    }

}
