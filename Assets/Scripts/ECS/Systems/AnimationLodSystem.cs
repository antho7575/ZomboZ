using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct AnimationLodSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        var em = s.EntityManager;
        float3 camPos = float3.zero; // wire up if you have a CameraTag

        var q = SystemAPI.QueryBuilder().WithAll<AnimationLod, LocalToWorld>().Build();
        using var entities = q.ToEntityArray(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var e = entities[i];
            var ltw = em.GetComponentData<LocalToWorld>(e);
            var lod = em.GetComponentData<AnimationLod>(e);

            float d2 = math.lengthsq(ltw.Position - camPos);
            // TODO: set a component to control anim/renderer LOD, then em.SetComponentData
        }
    }
}
