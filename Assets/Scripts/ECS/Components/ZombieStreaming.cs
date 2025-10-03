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