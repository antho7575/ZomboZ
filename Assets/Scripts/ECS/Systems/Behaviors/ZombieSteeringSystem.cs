using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieWanderSystem))]          // make it run after the writer of DesiredVelocity
[UpdateBefore(typeof(TransformSystemGroup))]        // you modify LocalTransform before transform systems
public partial struct ZombieSteeringSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState s)
    {
        // Only run when these exist; avoids scheduling empty jobs
        s.RequireForUpdate<ZombieTag>();
        s.RequireForUpdate<DesiredVelocity>();
        s.RequireForUpdate<Velocity>();
        s.RequireForUpdate<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var dt = SystemAPI.Time.DeltaTime;

        // Schedule a parallel job — no ComponentLookup, no ToEntityArray allocs
        s.Dependency = new SteeringJob
        {
            dt = dt,
            smoothing = 0.25f        // your lerp factor
        }.ScheduleParallel(s.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(ZombieTag))]   // only zombies
    partial struct SteeringJob : IJobEntity
    {
        public float dt;
        public float smoothing;

        // Writes: Velocity, LocalTransform
        // Reads : DesiredVelocity
        void Execute(ref LocalTransform xf,
                     ref Velocity v,
                     in DesiredVelocity dv)
        {
            v.Value = math.lerp(v.Value, dv.Value, smoothing);
            xf.Position += v.Value * dt;     // integrate
        }
    }
}
