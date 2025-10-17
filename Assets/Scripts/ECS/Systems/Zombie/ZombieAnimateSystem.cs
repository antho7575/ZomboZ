using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

// Rukhanka namespaces (adjust if yours differ)
using Rukhanka;                  // for FastAnimatorParameter

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial struct ZombieAnimateSystem : ISystem
{
    // cache parameter IDs (hashed once)
    FastAnimatorParameter speedParam;

    public void OnCreate(ref SystemState state)
    {
        // Name must match your controller parameter exactly
        speedParam = new FastAnimatorParameter("Speed");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new SetAnimParamsJob
        {
            speedParam = speedParam
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    // Only run for entities that have Rukhanka's AnimatorParametersAspect AND your anim state
    // and only when AnimState changed (saves lots of work).
    [BurstCompile]
    [WithChangeFilter(typeof(AnimState))]
    partial struct SetAnimParamsJob : IJobEntity
    {
        public FastAnimatorParameter speedParam;

        // The aspect binds to the animator on the entity; no GameObject/Animator calls here.
        void Execute(AnimatorParametersAspect animParams, in AnimState anim)
        {
            animParams.SetParameterValue(speedParam, anim.Speed);
        }
    }
}