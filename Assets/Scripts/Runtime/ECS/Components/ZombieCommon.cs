using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

// Persistent GUID for an active zombie entity (maps back to persisted record)
public struct ZombieGuid : IComponentData
{
    public Guid Value;
}
