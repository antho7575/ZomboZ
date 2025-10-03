using Unity.Entities;
using UnityEngine;

public struct CullingSettings : IComponentData { public float MaxVisibleRadius; }
public struct CullingSettingsTag : IComponentData { }

public class CullingSettingsAuthoring : MonoBehaviour
{
    public float MaxVisibleRadius = 20f;

    class Baker : Baker<CullingSettingsAuthoring>
    {
        public override void Bake(CullingSettingsAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new CullingSettings { MaxVisibleRadius = a.MaxVisibleRadius });
            AddComponent(e, new CullingSettingsTag());
        }
    }
}
