using Unity.Entities;
using UnityEngine;

public class ZombiePrefabAuthoring : MonoBehaviour
{
    [Header("Prefab to convert to an Entity Prefab")]
    public GameObject ZombiePrefab;

    class Baker : Baker<ZombiePrefabAuthoring>
    {
        public override void Bake(ZombiePrefabAuthoring authoring)
        {
            if (authoring.ZombiePrefab == null)
            {
                Debug.LogError("ZombiePrefabAuthoring: Assign a GameObject prefab.");
                return;
            }

            // Convert the GameObject prefab into an Entity prefab
            var prefabEntity = GetEntity(authoring.ZombiePrefab, TransformUsageFlags.Renderable);

            // Create a singleton entity that holds the prefab reference
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ZombiePrefabRef { Prefab = prefabEntity });

        }
    }
}
