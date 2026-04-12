using System;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Data used to create a zombie instance.
/// </summary>
public struct ZombieCreateRequest
{
    public Entity Prefab { get; set; }
    public Guid Id { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float Scale { get; set; }
    public float MoveSpeed { get; set; }
    public float Hunger { get; set; }
    public float3 Velocity { get; set; }
    public float3 DesiredVelocity { get; set; }
    public float TimeSinceSeenPlayer { get; set; }
    public bool WithWander { get; set; }
    public bool WithAnimation { get; set; }
}