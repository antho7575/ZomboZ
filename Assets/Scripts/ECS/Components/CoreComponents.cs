using Unity.Entities;
using Unity.Mathematics;

public struct NpcTag : IComponentData {}
public struct PlayerTag : IComponentData {}

public struct Health : IComponentData { public float Value; }
public struct Hunger : IComponentData { public float Value; }
public struct Stamina : IComponentData { public float Value; }

public struct AnimationLod : IComponentData { public float NearSq; public float MidSq; }

public struct WorldChunk : IComponentData { public int2 Coord; }
public struct ProceduralSeed : IComponentData { public uint Value; }

public struct GridConfig : IComponentData { public int2 Size; public float CellSize; public float2 OriginWS; }
public struct FlowFieldTag : IComponentData {}
public struct TargetCell : IComponentData { public int2 Cell; }
