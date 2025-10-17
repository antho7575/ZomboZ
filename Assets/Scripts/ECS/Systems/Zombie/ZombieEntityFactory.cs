using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Data used to create a zombie instance.
/// </summary>
public struct ZombieCreateRequest
{
    public Entity Prefab;
    public float3 Position;
    public quaternion Rotation;
    public float Scale;

    public float MoveSpeed;
    public float Hunger;
    public float3 Velocity;
    public float3 DesiredVelocity;
    public float TimeSinceSeenPlayer;

    public bool WithWander;
    public bool WithAnimation;
}

/// <summary>
/// Helper for creating zombies with consistent components.
/// </summary>
public static class ZombieEntityFactory
{
    public static Entity CreateZombie(EntityManager em, in ZombieCreateRequest req)
    {
        var e = em.Instantiate(req.Prefab);

        // Base transform
        em.SetComponentData(e, LocalTransform.FromPositionRotationScale(req.Position, req.Rotation, req.Scale));

        // Tags & behavior
        em.AddComponent<ZombieTag>(e);
        if (req.WithWander)
        {
            em.AddComponent<WanderTag>(e);
            em.AddComponent<WanderState>(e);
        }

        if (req.WithAnimation)
            em.AddComponent<AnimState>(e);

        // Movement & AI data
        em.AddComponentData(e, new Velocity { Value = req.Velocity });
        em.AddComponentData(e, new DesiredVelocity { Value = req.DesiredVelocity });
        em.AddComponentData(e, new MoveSpeed { Value = req.MoveSpeed });
        em.AddComponentData(e, new ZombieBlackboard
        {
            TimeSinceSeenPlayer = req.TimeSinceSeenPlayer,
            LastKnownPlayerPos = req.Position,
            Hunger = req.Hunger
        });

        return e;
    }
}
