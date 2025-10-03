using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieUtilitySelectorSystem))] // run after you pick tags
public partial struct ZombieWanderSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // Add WanderState once for entities that need it
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach (var (xf, entity) in SystemAPI
                 .Query<RefRO<LocalTransform>>()
                 .WithAll<ZombieTag, WanderTag>()
                 .WithNone<WanderState>()
                 .WithEntityAccess())
        {
            ecb.AddComponent(entity, new WanderState { Target = xf.ValueRO.Position, RepathTimer = 0f });
        }
        ecb.Playback(s.EntityManager);
        ecb.Dispose();

        // Drive DesiredVelocity for wanderers
        foreach (var (xf, spd, dv, wsRef, entity) in SystemAPI
                 .Query<RefRO<LocalTransform>, RefRO<MoveSpeed>, RefRW<DesiredVelocity>, RefRW<WanderState>>()
                 .WithAll<ZombieTag, WanderTag>()
                 .WithEntityAccess())
        {
            var pos = xf.ValueRO.Position;
            var ws = wsRef.ValueRO;
            float timeLeft = ws.RepathTimer - dt;

            // need a new target?
            float2 p2 = pos.xz;
            float2 t2 = new float2(ws.Target.x, ws.Target.z);
            float dist = math.distance(p2, t2);
            if (timeLeft <= 0f || dist < 0.5f)
            {
                // lightweight deterministic RNG from entity index + time
                uint seed = (uint)(entity.Index ^ (int)(SystemAPI.Time.ElapsedTime * 997.0f)) | 1u;
                var r = Unity.Mathematics.Random.CreateFromIndex(seed);
                float radius = r.NextFloat(3f, 8f);
                float angle = r.NextFloat(0f, 6.2831853f); // 0..2π
                float3 offset = new float3(math.cos(angle), 0, math.sin(angle)) * radius;

                ws.Target = pos + offset;
                timeLeft = r.NextFloat(1.5f, 3.5f);
            }

            // steer toward target
            float3 dir = ws.Target - pos; dir.y = 0f;
            float3 desired = math.normalizesafe(dir) * math.max(0.01f, spd.ValueRO.Value);

            dv.ValueRW.Value = desired;
            wsRef.ValueRW = new WanderState { Target = ws.Target, RepathTimer = timeLeft };
        }
    }
}
