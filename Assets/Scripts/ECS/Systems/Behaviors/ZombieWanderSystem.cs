using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieUtilitySelectorSystem))]
public partial struct ZombieWanderSystem : ISystem
{
    private EntityQuery _wanderQuery;

    private ComponentLookup<LocalTransform> _xfLookup;
    private ComponentLookup<MoveSpeed> _spdLookup;
    private ComponentLookup<DesiredVelocity> _dvLookup;
    private ComponentLookup<WanderState> _wsLookup;

    public void OnCreate(ref SystemState s)
    {
        _wanderQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieTag, WanderTag, LocalTransform, MoveSpeed, DesiredVelocity>()
            .Build(ref s);

        _xfLookup = s.GetComponentLookup<LocalTransform>(true);
        _spdLookup = s.GetComponentLookup<MoveSpeed>(true);
        _dvLookup = s.GetComponentLookup<DesiredVelocity>(false);
        _wsLookup = s.GetComponentLookup<WanderState>(false);
    }

    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // refresh lookups each frame
        _xfLookup.Update(ref s);
        _spdLookup.Update(ref s);
        _dvLookup.Update(ref s);
        _wsLookup.Update(ref s);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        using var ents = _wanderQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];

            // ensure WanderState exists once
            if (!_wsLookup.HasComponent(e))
            {
                var p = _xfLookup[e].Position;
                ecb.AddComponent(e, new WanderState { Target = p, RepathTimer = 0f });
                continue; // will start next frame
            }

            var xf = _xfLookup[e];
            var spd = _spdLookup[e];
            var ws = _wsLookup[e];

            // repath?
            ws.RepathTimer -= dt;
            float2 p2 = xf.Position.xz;
            float2 t2 = new float2(ws.Target.x, ws.Target.z);
            float dist = math.distance(p2, t2);

            if (ws.RepathTimer <= 0f || dist < 0.5f)
            {
                // light RNG using entity index + time
                uint seed = (uint)(e.Index ^ (int)(SystemAPI.Time.ElapsedTime * 997.0f)) | 1u;
                var r = Unity.Mathematics.Random.CreateFromIndex(seed);
                float radius = r.NextFloat(3f, 8f);
                float angle = r.NextFloat(0f, 6.2831853f);
                float3 offset = new float3(math.cos(angle), 0, math.sin(angle)) * radius;

                ws.Target = xf.Position + offset;
                ws.RepathTimer = r.NextFloat(1.5f, 3.5f);
            }

            // write desired velocity
            float3 dir = ws.Target - xf.Position; dir.y = 0f;
            var dv = _dvLookup[e];
            dv.Value = math.normalizesafe(dir) * math.max(0.01f, spd.Value);
            _dvLookup[e] = dv;

            // write back state
            _wsLookup[e] = ws;
        }

        ecb.Playback(s.EntityManager);
        ecb.Dispose();
    }
}