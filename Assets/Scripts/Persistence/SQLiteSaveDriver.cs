using System.Collections.Generic;
using Unity.Mathematics;

public class SQLiteSaveDriver : ISaveDriver
{
    public void Open(string path) { /* TODO: open sqlite connection, create tables */ }
    public void Close() { /* TODO: dispose */ }

    public ChunkMeta LoadChunkMeta(int2 chunk) { return default; }
    public void SaveChunkMeta(ChunkMeta meta) { }

    public IEnumerable<EntityRow> LoadEntitiesByChunk(int2 chunk) { yield break; }
    public void SaveEntitiesBatch(int2 chunk, IEnumerable<EntityRow> rows) { }

    public IEnumerable<ContainerRow> LoadContainers(int2 chunk) { yield break; }
    public void SaveContainersBatch(int2 chunk, IEnumerable<ContainerRow> rows) { }
}
