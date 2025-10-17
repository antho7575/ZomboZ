using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ZombieWanderSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<ZombieTag>();
        s.RequireForUpdate<WanderTag>();
        s.RequireForUpdate<WanderState>(); // guarantees the job runs only if WanderState exists
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var timeSeed = (float)SystemAPI.Time.ElapsedTime;

        s.Dependency = new WanderJob
        {
            dt = dt,
            timeSeed = timeSeed
        }.ScheduleParallel(s.Dependency);
    }

    [WithAll(typeof(ZombieTag), typeof(WanderTag), typeof(WanderState))]
    partial struct WanderJob : IJobEntity
    {
        public float dt;
        public float timeSeed;

        void Execute(ref WanderState ws,
                     ref DesiredVelocity dv,
                     ref AnimState anim,     
                     in LocalTransform xf,
                     in MoveSpeed spd,
                     Entity e)
        {
            if (ws.Initialized == 0)
            {
                ws.Target = xf.Position;
                ws.RepathTimer = 0f;
                ws.Initialized = 1;
            }

            ws.RepathTimer -= dt;

            float2 p2 = new float2(xf.Position.x, xf.Position.z);
            float2 t2 = new float2(ws.Target.x, ws.Target.z);
            float dist2 = math.lengthsq(p2 - t2);

            if (ws.RepathTimer <= 0f || dist2 < 0.25f)
            {
                uint seed = (uint)(e.Index ^ (int)(timeSeed * 997.0f)) | 1u;
                var r = Unity.Mathematics.Random.CreateFromIndex(seed);

                float radius = r.NextFloat(3f, 8f);
                float angle = r.NextFloat(0f, 2f * math.PI);
                float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * radius;

                ws.Target = xf.Position + offset;
                ws.RepathTimer = r.NextFloat(1.5f, 3.5f);
            }

            float3 dir = ws.Target - xf.Position; dir.y = 0f;
            float3 desired = math.normalizesafe(dir) * math.max(0.01f, spd.Value);
            dv.Value = desired;

            // Feed the Animator speed (no units conversion needed if spd is m/s):
            anim.Speed = math.length(desired);
        }
    }

}
