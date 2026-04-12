using System;
using Unity.Entities;
using Unity.Transforms;

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

        // Attach persisted Guid if provided
        if (req.Id != Guid.Empty)
        {
            em.AddComponentData(e, new ZombieGuid { Value = req.Id });
        }

        return e;
    }
}
