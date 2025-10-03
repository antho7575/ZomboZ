using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ZombieWanderSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;

        var q = SystemAPI.QueryBuilder()
            .WithAll<ZombieTag, WanderTag, LocalTransform, MoveSpeed, DesiredVelocity>()
            .Build();

        using var ents = q.ToEntityArray(Allocator.Temp);

        // Ensure WanderState exists once
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
            if (!em.HasComponent<WanderState>(ents[i]))
                ecb.AddComponent(ents[i], new WanderState { Target = em.GetComponentData<LocalTransform>(ents[i]).Position, RepathTimer = 0f });
        ecb.Playback(em); ecb.Dispose();

        // Update
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var xf = em.GetComponentData<LocalTransform>(e);
            var spd = em.GetComponentData<MoveSpeed>(e);
            var dv = em.GetComponentData<DesiredVelocity>(e);
            var ws = em.GetComponentData<WanderState>(e);

            ws.RepathTimer -= dt;
            float dist = math.distance(xf.Position.xz, ws.Target.xz);
            if (ws.RepathTimer <= 0f || dist < 0.5f)
            {
                uint seed = (uint)(e.Index * 9781 ^ (int)(SystemAPI.Time.ElapsedTime * 4919)) | 1u;
                var r = Unity.Mathematics.Random.CreateFromIndex(seed);
                float radius = r.NextFloat(3f, 8f);
                float angle = r.NextFloat(0f, 6.2831853f);
                ws.Target = xf.Position + new float3(math.cos(angle), 0, math.sin(angle)) * radius;
                ws.RepathTimer = r.NextFloat(1.5f, 3.5f);
            }

            float3 dir = ws.Target - xf.Position; dir.y = 0;
            dv.Value = math.normalizesafe(dir) * spd.Value;

            em.SetComponentData(e, dv);
            em.SetComponentData(e, ws);
        }
    }
}
