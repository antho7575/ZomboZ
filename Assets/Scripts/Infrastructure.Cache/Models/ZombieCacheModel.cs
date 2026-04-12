using System;
using Unity.Mathematics;

namespace ZomboZ.Infrastructure.Cache
{
    public class ZombieCacheModel
    {
        public Guid Id { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public long LastSeenTicks { get; set; }
        public bool IsSpawned { get; set; }
        public float3 Position => new float3(PosX, PosY, PosZ);
    }
}
