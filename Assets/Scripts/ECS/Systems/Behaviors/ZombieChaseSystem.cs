using Unity.Entities;
using Unity.Mathematics;

public partial struct ZombieChaseSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;

        var q = SystemAPI.QueryBuilder()
            .WithAll<ZombieTag, ChaseTag, ZombieBlackboard, MoveSpeed, DesiredVelocity>()
            .Build();

        using var ents = q.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var bb = em.GetComponentData<ZombieBlackboard>(e);
            var sp = em.GetComponentData<MoveSpeed>(e);
            var dv = em.GetComponentData<DesiredVelocity>(e);

            float3 dir = bb.LastKnownPlayerPos - dv.Value; // temp; we’ll overwrite dv.Value next
            dir = bb.LastKnownPlayerPos - em.GetComponentData<DesiredVelocity>(e).Value; // (not used; see below)

            // Just steer to last known pos
            float3 to = bb.LastKnownPlayerPos - em.GetComponentData<DesiredVelocity>(e).Value; // safe but noisy
            dir = bb.LastKnownPlayerPos - em.GetComponentData<DesiredVelocity>(e).Value;

            // Simpler: write directly
            float3 desiredDir = bb.LastKnownPlayerPos - em.GetComponentData<DesiredVelocity>(e).Value;

            // Proper:
            float3 desired = bb.LastKnownPlayerPos - em.GetComponentData<DesiredVelocity>(e).Value;

            // Cleaner version:
            float3 desiredV = float3.zero;
            {
                var pos = s.EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(e).Position;
                var d = bb.LastKnownPlayerPos - pos; d.y = 0;
                desiredV = math.normalizesafe(d) * sp.Value;
            }

            dv.Value = desiredV;
            em.SetComponentData(e, dv);
        }
    }
}
