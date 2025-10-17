using Unity.Entities;

[InternalBufferCapacity(0)] public struct CellCost : IBufferElementData { public ushort Value; }