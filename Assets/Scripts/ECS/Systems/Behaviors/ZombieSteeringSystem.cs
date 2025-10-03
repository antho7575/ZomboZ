using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ✅ Run in Simulation and BEFORE TransformSystemGroup so we write LocalTransform
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ZombieSteeringSystem : ISystem
{
    private EntityQuery _steerQuery;
    private ComponentLookup<LocalTransform> _xfLookup;
    private ComponentLookup<Velocity> _vLookup;
    private ComponentLookup<DesiredVelocity> _dvLookup;

    public void OnCreate(ref SystemState s)
    {
        _steerQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<ZombieTag, LocalTransform, Velocity, DesiredVelocity>()
            .Build(ref s);

        _xfLookup = s.GetComponentLookup<LocalTransform>(false);   // write
        _vLookup = s.GetComponentLookup<Velocity>(false);          // write
        _dvLookup = s.GetComponentLookup<DesiredVelocity>(true);    // read
    }

    public void OnUpdate(ref SystemState s)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // Refresh lookups each frame
        _xfLookup.Update(ref s);
        _vLookup.Update(ref s);
        _dvLookup.Update(ref s);

        // Ensure we’re not racing with prior readers
        // (usually ordering is enough; this is extra safety if you still see warnings)
        // s.Dependency.Complete();

        using var ents = _steerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            var e = ents[i];
            var xf = _xfLookup[e];
            var v = _vLookup[e];
            var dv = _dvLookup[e];

            v.Value = math.lerp(v.Value, dv.Value, 0.25f);
            xf.Position += v.Value * dt;

            _vLookup[e] = v;
            _xfLookup[e] = xf;
        }
    }
}
