using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))] // runs after Simulation
public partial struct ZombieSteeringSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (xfRef, vRef, dv) in SystemAPI
                 .Query<RefRW<LocalTransform>, RefRW<Velocity>, RefRO<DesiredVelocity>>()
                 .WithAll<ZombieTag>())
        {
            var v = vRef.ValueRO.Value;
            var vd = dv.ValueRO.Value;

            v = math.lerp(v, vd, 0.25f);
            var xf = xfRef.ValueRO;
            xf.Position += v * dt;

            vRef.ValueRW.Value = v;
            xfRef.ValueRW = xf;
        }
    }
}
