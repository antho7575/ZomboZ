using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(0)] public struct CellCost : IBufferElementData { public ushort Value; }
[InternalBufferCapacity(0)] public struct CellIntegration : IBufferElementData { public uint Value; }
[InternalBufferCapacity(0)] public struct CellDir : IBufferElementData { public float2 Value; }
[InternalBufferCapacity(8)] public struct InventoryItem : IBufferElementData { public int ItemId; public int Count; }
