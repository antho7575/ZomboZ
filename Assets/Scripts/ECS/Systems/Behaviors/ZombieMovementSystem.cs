using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Handles zombie movement and facing direction based on DesiredVelocity.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieWanderSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ZombieMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<ZombieTag>();
        s.RequireForUpdate<DesiredVelocity>();
        s.RequireForUpdate<Velocity>();
        s.RequireForUpdate<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var dt = math.max(SystemAPI.Time.DeltaTime, 1e-6f);

        s.Dependency = new MoveAndFaceJob
        {
            dt = dt,
            velLerp = 0.25f,  // how fast to approach DesiredVelocity
            rotSpeed = 10f    // how fast to turn (slerp)
        }.ScheduleParallel(s.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(ZombieTag))]
    partial struct MoveAndFaceJob : IJobEntity
    {
        public float dt;
        public float velLerp;
        public float rotSpeed;

        void Execute(ref LocalTransform xf, ref Velocity v, in DesiredVelocity dv)
        {
            // 1️⃣ Smooth velocity toward target
            v.Value = math.lerp(v.Value, dv.Value, velLerp);

            // 2️⃣ Integrate position
            xf.Position += v.Value * dt;

            // 3️⃣ Rotate to face movement direction
            float3 dir = v.Value;
            dir.y = 0f; // keep upright
            float lenSq = math.lengthsq(dir);
            if (lenSq > 1e-6f)
            {
                float3 fwd = math.normalize(dir);
                quaternion targetRot = quaternion.LookRotationSafe(fwd, math.up());
                float t = math.saturate(rotSpeed * dt);
                xf.Rotation = math.slerp(xf.Rotation, targetRot, t);
            }
        }
    }
}
