using Unity.Entities;
using UnityEngine;

public struct PrimaryRenderEntity : IComponentData { public Entity Value; }

public class PrimaryRenderEntityAuthoring : MonoBehaviour
{
    class Baker : Baker<PrimaryRenderEntityAuthoring>
    {
        public override void Bake(PrimaryRenderEntityAuthoring a)
        {
            var root = GetEntity(TransformUsageFlags.None);

            // find first renderer child and get its ECS entity
            Entity renderE = Entity.Null;
            var mr = a.GetComponentInChildren<MeshRenderer>(true);
            if (mr != null) renderE = GetEntity(mr, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            else
            {
                var smr = a.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null) renderE = GetEntity(smr, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            }

            AddComponent(root, new PrimaryRenderEntity { Value = renderE });
        }
    }
}
