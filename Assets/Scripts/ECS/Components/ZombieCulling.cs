using Unity.Entities;
using Unity.Mathematics;

// CPU gate (AI/animation/physics only run when enabled)
public struct ZombieActive : IComponentData, IEnableableComponent { }

// Stamp to know if we refreshed this frame
public struct ActiveStamp : IComponentData { public uint Frame; }

// Shared grid cell for spatial filtering
public struct GridCell : ISharedComponentData
{
    public int2 Coord;
}

public struct FocusPoint : IComponentData
{
    public float3 Value;
}