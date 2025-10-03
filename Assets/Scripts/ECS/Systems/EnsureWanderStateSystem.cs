using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ZombieUtilitySelectorSystem))]
public partial struct EnsureWanderStateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<ZombieTag>();
        s.RequireForUpdate<WanderTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(s.WorldUnmanaged).AsParallelWriter();

        var job = new AddWanderStateJob { ecb = ecb };
        s.Dependency = job.ScheduleParallel(s.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(ZombieTag), typeof(WanderTag))]
    [WithNone(typeof(WanderState))]
    partial struct AddWanderStateJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        void Execute([EntityIndexInQuery] int sortKey, Entity e, in LocalTransform xf)
        {
            ecb.AddComponent(sortKey, e, new WanderState
            {
                Target = xf.Position,
                RepathTimer = 0f
            });
        }
    }
}
