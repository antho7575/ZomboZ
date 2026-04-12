using Unity.Entities;
using Unity.Mathematics;

namespace ZomboZ.Runtime
{
    public struct CameraPositionSingleton : IComponentData
    {
        public float3 Value;
    }
}