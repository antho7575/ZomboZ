using Unity.Mathematics;

public struct ChunkMeta { public string Key; public int2 Coord; public uint Seed; public bool Visited; public int LastSeenDay; }
public struct EntityRow { public System.Guid Id; public string Kind; public string ChunkKey; public byte[] ComponentsBlob; public bool Alive; }
public struct ContainerRow { public System.Guid Id; public string ChunkKey; public byte[] ItemsBlob; public bool Opened; }
