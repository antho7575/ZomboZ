using Unity.Entities;

public partial struct ZombieIdleSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        var q = SystemAPI.QueryBuilder().WithAll<ZombieTag, IdleTag, DesiredVelocity>().Build();
        using var ents = q.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var dv = em.GetComponentData<DesiredVelocity>(e);
            dv.Value = default; // stand still
            em.SetComponentData(e, dv);
        }
    }
}
