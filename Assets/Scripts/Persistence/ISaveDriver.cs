using System.Collections.Generic;
using Unity.Mathematics;

public interface ISaveDriver
{
    void Open(string path);
    void Close();

    ChunkMeta LoadChunkMeta(int2 chunk);
    void SaveChunkMeta(ChunkMeta meta);

    IEnumerable<EntityRow> LoadEntitiesByChunk(int2 chunk);
    void SaveEntitiesBatch(int2 chunk, IEnumerable<EntityRow> rows);

    IEnumerable<ContainerRow> LoadContainers(int2 chunk);
    void SaveContainersBatch(int2 chunk, IEnumerable<ContainerRow> rows);
}
