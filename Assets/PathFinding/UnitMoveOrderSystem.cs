using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

public class UnitMoveOrderSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        m_EndSimulationEcbSystem = World
               .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        Vector3 mousePosition = Utils.INSTANCE.getMouseWorldPosition3D();

        float cellSize = PathFindingGridSetup.INSTANCE.pathFindingGrid.getCellSize();
        float3 originPositionGrid = PathFindingGridSetup.INSTANCE.pathFindingGrid.getOriginPosition();
        int widthGrid = PathFindingGridSetup.INSTANCE.pathFindingGrid.getWidth();
        int heightGrid = PathFindingGridSetup.INSTANCE.pathFindingGrid.getHeight();

        //находим координаты сетки, соответствующие мировой позиции
        getXZ(mousePosition, originPositionGrid, cellSize, out int endX, out int endY);
        //если координата лежит вне сетки, то выравниваем ее в рамках текущей размерности сетки 
        validateGridPosition(widthGrid, heightGrid, ref endX, ref endY);


        //в случае возникновения события к сущностям добавляем компонент с информацией о текущей и целевой позицией 
        if (Input.GetMouseButtonDown(0))
        {
            EntityCommandBuffer ecb = m_EndSimulationEcbSystem.CreateCommandBuffer();

            Entities
            .WithName("UnitMoveOrderSystem")
            .ForEach((Entity entity, DynamicBuffer<PathPositionBuffer> pathPositionBuffers, ref Translation position) =>
            {
                getXZ(position.Value, originPositionGrid, cellSize, out int startX, out int startY);
                validateGridPosition(widthGrid, heightGrid, ref startX, ref startY);

                ecb.AddComponent(entity, new PathFindingParams { startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) });
            })
            .Schedule();

            m_EndSimulationEcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }

    private static void validateGridPosition(int width, int height, ref int x, ref int z)
    {
        x = math.clamp(x, 0, width - 1);
        z = math.clamp(z, 0, height - 1);
    }

    private static void getXZ(float3 worldPosition, float3 originPosition, float cellSize, out int x, out int z)
    {
        x = (int)((worldPosition - originPosition).x / cellSize);
        z = (int)((worldPosition - originPosition).z / cellSize);
    }
}
