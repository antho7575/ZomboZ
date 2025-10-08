using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
public enum MapSizePreset : byte { Small, Medium, Large }
public enum BiomeId : byte { Plains, Forest, Desert, Snow, Water }
public struct WorldGenConfig : IComponentData
{
    public MapSizePreset Size;
    public float CellSize;    // meters per tile (keep same as your streaming cell if you want 1:1)
    public uint Seed;
    public float HeightScale; // meters of relief
    public float MoistureScale;

    // Noise frequency (lower = larger features)
    public float HeightFreq;
    public float MoistureFreq;
}

public struct WorldGenRequest : IComponentData { } // tag to (re)generate once

// Dynamic buffer of biome weights (normalized at bake)
public struct BiomeWeight : IBufferElementData
{
    public BiomeId Id;
    public float Weight; // relative weight (not required to be normalized in authoring)
}



public struct MapGridBlob
{
    public int2 size;       // tiles (width,height)
    public float cellSize;  // meters per tile
    // Fields per tile (flattened row-major)
    public BlobArray<float> height;    // 0..1 (scaled later by HeightScale)
    public BlobArray<float> moisture;  // 0..1
    public BlobArray<byte> biome;     // BiomeId
    public BlobArray<byte> flags;     // bitfield: bits for river/road/etc (reserved for next passes)
}

// Singleton that stores the blob reference
public struct MapGridSingleton : IComponentData
{
    public BlobAssetReference<MapGridBlob> Grid;
}