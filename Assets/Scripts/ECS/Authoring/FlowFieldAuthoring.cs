using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlowFieldAuthoring : MonoBehaviour
{
    public int Width = 64;
    public int Height = 64;
    public float CellSize = 1f;
    public float2 OriginWS = float2.zero;

    class Baker : Baker<FlowFieldAuthoring>
    {
        public override void Bake(FlowFieldAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);

            AddComponent(e, new GridConfig
            {
                Size = new int2(a.Width, a.Height),
                CellSize = a.CellSize,
                OriginWS = a.OriginWS
            });

            AddBuffer<CellCost>(e);
            AddBuffer<CellIntegration>(e);
            AddBuffer<CellDir>(e);
            AddComponent<FlowFieldTag>(e);
        }
    }
}
