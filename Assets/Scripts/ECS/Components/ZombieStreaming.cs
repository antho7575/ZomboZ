using Unity.Entities;
using Unity.Mathematics;

public struct ZombieId : IComponentData { public int Value; }

public struct Sector : IComponentData { public int2 XY; }

public struct ObserverSettings : IComponentData
{
    public float SectorSize;      // e.g. 64
    public int LoadRadius;      // e.g. 2
    public int HardUnloadRadius;// e.g. 3
}

public struct StreamingManaged : IComponentData { }

// Tag each spawned zombie with the cell it belongs to (Shared so we can unload by cell fast)
public struct CellId : ISharedComponentData
{
    public int2 Coord;
}

// Blob: grid → [start,len] into one flat positions array
public struct ZombieIndexBlob
{
    public int2 minCoord;   // inclusive
    public int2 size;       // width,height in cells
    public float cellSize;  // meters per cell
    public BlobArray<int> cellStart;     // size.x * size.y
    public BlobArray<int> cellLen;       // size.x * size.y
    public BlobArray<float3> positions;  // concatenated positions for all cells
}

// Runtime config/state singleton
public struct ZombieStreamConfig : IComponentData
{
    public BlobAssetReference<ZombieIndexBlob> Index; // assumed defined elsewhere
    public Entity Prefab;
    public int LoadRadiusCells;     // base spawn radius (in cells)
    public int VisibleMarginCells;  // inner-visible margin: Rvis = Rload - VisibleMarginCells
    public float LookaheadSeconds;    // speed-based lookahead; 0 disables (e.g., 0.35f)
}

public struct ZombieStreamState : IComponentData
{
    public int2 LastCenterCell;      // int.MinValue on first run
    public float3 LastCenterWS;       // to estimate speed
}

// Keep the set of currently loaded cells on the singleton so we can diff cheaply
public struct LoadedCell : IBufferElementData
{
    public int2 Coord;
}

