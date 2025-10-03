using Unity.Entities;
using Unity.Mathematics;

// Active behavior tags (mutually exclusive)
public struct IdleTag : IComponentData { }
public struct WanderTag : IComponentData { }
public struct ChaseTag : IComponentData { }

// Per-behavior state (if needed)
public struct WanderState : IComponentData
{
    public float3 Target;
    public float RepathTimer;
}
