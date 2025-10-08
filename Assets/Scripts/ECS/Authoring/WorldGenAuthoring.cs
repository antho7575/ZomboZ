using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class WorldGenAuthoring : MonoBehaviour
{
    [Header("Size / Resolution")]
    public MapSizePreset Size = MapSizePreset.Medium;
    [Tooltip("Meters per tile (32–64 works well)")]
    public float CellSize = 32f;
    [Tooltip("Deterministic seed")]
    public uint Seed = 1;

    [Header("Height / Moisture")]
    public float HeightScale = 40f;
    public float MoistureScale = 1f;
    [Tooltip("Lower frequency = broader features")]
    public float HeightFrequency = 0.0025f;
    public float MoistureFrequency = 0.0030f;

    [Header("Biome Mix (weights)")]
    public float Plains = 1.0f;
    public float Forest = 1.0f;
    public float Desert = 0.7f;
    public float Snow = 0.5f;
    public float Water = 0.3f;

    [Header("Generate on load")]
    public bool GenerateAtStart = true;

    class Baker : Baker<WorldGenAuthoring>
    {
        public override void Bake(WorldGenAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);

            AddComponent(e, new WorldGenConfig
            {
                Size = a.Size,
                CellSize = math.max(1f, a.CellSize),
                Seed = a.Seed == 0 ? 1u : a.Seed,
                HeightScale = a.HeightScale,
                MoistureScale = a.MoistureScale,
                HeightFreq = math.max(1e-5f, a.HeightFrequency),
                MoistureFreq = math.max(1e-5f, a.MoistureFrequency)
            });

            var buf = AddBuffer<BiomeWeight>(e);
            // Push raw weights…
            buf.Add(new BiomeWeight { Id = BiomeId.Plains, Weight = math.max(0f, a.Plains) });
            buf.Add(new BiomeWeight { Id = BiomeId.Forest, Weight = math.max(0f, a.Forest) });
            buf.Add(new BiomeWeight { Id = BiomeId.Desert, Weight = math.max(0f, a.Desert) });
            buf.Add(new BiomeWeight { Id = BiomeId.Snow, Weight = math.max(0f, a.Snow) });
            buf.Add(new BiomeWeight { Id = BiomeId.Water, Weight = math.max(0f, a.Water) });

            // Normalize weights at bake
            float sum = 0f; for (int i = 0; i < buf.Length; i++) sum += buf[i].Weight;
            if (sum <= 1e-6f) { for (int i = 0; i < buf.Length; i++) buf[i] = new BiomeWeight { Id = buf[i].Id, Weight = 1f / buf.Length }; }
            else { for (int i = 0; i < buf.Length; i++) buf[i] = new BiomeWeight { Id = buf[i].Id, Weight = buf[i].Weight / sum }; }

            if (a.GenerateAtStart)
                AddComponent<WorldGenRequest>(e);
        }
    }
}
