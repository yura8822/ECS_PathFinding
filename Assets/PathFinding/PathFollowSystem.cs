using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

public class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        float cellSize = PathFindingGridSetup.INSTANCE.pathFindingGrid.getCellSize();
        float3 originPosition = PathFindingGridSetup.INSTANCE.pathFindingGrid.getOriginPosition();


        Entities
        .WithName("PathFollowSystem")
        .ForEach((DynamicBuffer<PathPositionBuffer> pathPositionBuffer, ref PhysicsVelocity physics, ref Rotation rotation, ref Translation position, ref PathFollowData pathFollowData, in CharacterData characterData) =>
        {
            if (pathFollowData.pathIndex >= 0)
            {
                int2 pathIndex = pathPositionBuffer[pathFollowData.pathIndex].position;
                float3 pathWorldPosition = getWorldPosition(pathIndex.x, pathIndex.y, cellSize, originPosition);
                float3 target = offsetPositionToCenter(pathWorldPosition, cellSize);

                float3 dir = target - position.Value;
                dir.y = 0; // блокируем ось y, для строго перпендикулярного направления вектора

                quaternion targetDirection = quaternion.LookRotation(dir, math.up());
                rotation.Value = math.slerp(rotation.Value, targetDirection, deltaTime * characterData.rotationSpeed);
                physics.Linear = math.forward(rotation.Value) * deltaTime * characterData.movementSpeed;

                if (math.distance(position.Value, target) < 3.5f)
                {
                    pathFollowData.pathIndex--;
                }
            }
            else
            {
                physics.Linear = 0;
            }
        }).Schedule();
    }

    public static float3 getWorldPosition(int x, int z, float cellSize, float3 originPosition)
    {
        float3 pos = new float3(x, 0, z) * cellSize + originPosition;
        return pos;
    }

    private static float3 offsetPositionToCenter(float3 position, float cellSize)
    {
        return position + new float3(cellSize * .5f, 0, cellSize * .5f);
    }
}
