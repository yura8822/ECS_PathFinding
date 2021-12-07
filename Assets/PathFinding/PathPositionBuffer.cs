using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(50)]
public struct PathPositionBuffer : IBufferElementData
{
    public int2 position;
}
