using Unity.Entities;
using Unity.Mathematics;

public struct ZombieTag : IComponentData { }
public struct MoveSpeed : IComponentData { public float Value; }
public struct Velocity : IComponentData { public float3 Value; }

// All behaviors write here; Steering reads it.
public struct DesiredVelocity : IComponentData { public float3 Value; }

// Optional: a small blackboard for the selector/behaviors
public struct ZombieBlackboard : IComponentData
{
    public float TimeSinceSeenPlayer;
    public float3 LastKnownPlayerPos;
    public float Hunger; // 0..1 if you like
}

// Simple LOD tags
public struct LODNear : IComponentData { }
public struct LODMid : IComponentData { }
public struct LODFar : IComponentData { }

// Store this for unloaded zombies
public struct ZombieRecord
{
    public int Id;
    public int2 Sector;
    public float3 Pos;
    public float Heading;       // radians
    public uint WanderSeed;
    public float TimeSinceSeen;  // for your utility selector later
}
