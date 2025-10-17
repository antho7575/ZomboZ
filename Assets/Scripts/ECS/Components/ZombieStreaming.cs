// --- Data ---

using Unity.Entities;
using Unity.Mathematics;

public struct ZombieStreamConfig : IComponentData
{
    public Entity Prefab;
    public float CellSize;
    public int VisibleRadiusCells;
}

public struct ZombieIndexRef : IComponentData
{
    public BlobAssetReference<ZombieIndexBlob> Blob;
}

public struct LoadedCell : IBufferElementData { public int2 Coord; }

// Blob: positions grouped by cell
public struct ZombieIndexBlob
{
    public BlobArray<float3> positions;
    public BlobArray<int2> uniqueCells;
    public BlobArray<int> cellStart;
    public BlobArray<int> cellLen;
}

// Optional: tag your player entity
public struct CellCoord : IComponentData { public int2 Value; }
