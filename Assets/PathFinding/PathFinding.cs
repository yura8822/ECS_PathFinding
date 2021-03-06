using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

public class PathFinding : ComponentSystem
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    protected override void OnUpdate()
    {
        int width = PathFindingGridSetup.INSTANCE.pathFindingGrid.getWidth();
        int height = PathFindingGridSetup.INSTANCE.pathFindingGrid.getHeight();
        int2 gridSize = new int2(width, height);

        NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        Entities.ForEach((Entity entity, DynamicBuffer<PathPositionBuffer> pathPositionBuffer, ref PathFindingParams pathFindingParams) =>
                {
                    FindPathJob findPathJob = new FindPathJob
                    {
                        pathNodeArray = getPathNodeArray(),
                        startPosition = pathFindingParams.startPosition,
                        endPosition = pathFindingParams.endPosition,
                        entity = entity,
                        pathFollowComponentFromEntity = GetComponentDataFromEntity<PathFollowData>(),
                        gridSize = gridSize,
                        pathPositionBuffer = pathPositionBuffer
                    };
                    jobHandleList.Add(findPathJob.Schedule());
                    PostUpdateCommands.RemoveComponent<PathFindingParams>(entity);
                });
                JobHandle.CompleteAll(jobHandleList);
    }
    private NativeArray<PathNode> getPathNodeArray()
    {
        _Grid<GridNode> grid = PathFindingGridSetup.INSTANCE.pathFindingGrid;

        int2 gridSize = new int2(grid.getWidth(), grid.getHeight());
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                PathNode pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = calculateIndex(x, y, gridSize.x);

                pathNode.gCost = int.MaxValue;

                pathNode.isWalkable = grid.getGridObject(x, y).IsWalkable();
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
        }
        return pathNodeArray;
    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        [DeallocateOnJobCompletion] public NativeArray<PathNode> pathNodeArray; //Allocator.TempJob ???????????????????? ???????????? pathNodeArray.Dispose();
        public int2 startPosition;
        public int2 endPosition;
        public Entity entity;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<PathFollowData> pathFollowComponentFromEntity;
        public int2 gridSize;
        [NativeDisableContainerSafetyRestriction]
        public DynamicBuffer<PathPositionBuffer> pathPositionBuffer;

        public void Execute()
        {
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                PathNode pathNode = pathNodeArray[i];
                pathNode.hCost = calculateDistanceCost(new int2(pathNode.x, pathNode.y), endPosition);
                pathNode.cameFromNodeIndex = -1;
                pathNodeArray[i] = pathNode;
            }
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); //left
            neighbourOffsetArray[1] = new int2(+1, 0); //right
            neighbourOffsetArray[2] = new int2(0, +1); //up
            neighbourOffsetArray[3] = new int2(0, -1); //down
            neighbourOffsetArray[4] = new int2(-1, -1); //left down
            neighbourOffsetArray[5] = new int2(-1, +1); //left up
            neighbourOffsetArray[6] = new int2(+1, -1); //right down
            neighbourOffsetArray[7] = new int2(+1, +1); //right up

            int endNodeIndex = calculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[calculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.calculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                int currentNodeIndex = getLowestCoatFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {  //???????? ????????????????????
                    break;
                }

                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        //?????????????? ???????? ????????????????, ?????????????? ?????? ???? openList
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                //?? ?????????????????? ?? ?????????????????????? ?? closedList
                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!isPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        //???????????????? ?????????????? ?????????????????? ???? ?????????????????? ??????????
                        continue;
                    }

                    int neighbourNodeIndex = calculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        //?????????????? ???????? ?????? ???????????????? ?? ?????????????????? ?? ???????????????? ????????????
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        // ???????? ???????????????? ????????????????????????
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

                    int tentativeGCost = currentNode.gCost + calculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.calculateFCost();
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }

            pathPositionBuffer.Clear();
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {
                //???????? ???? ????????????
                pathFollowComponentFromEntity[entity] = new PathFollowData { pathIndex = -1 };
            }
            else
            {
                //???????? ????????????
                calculatePath(pathNodeArray, endNode, pathPositionBuffer);
                pathFollowComponentFromEntity[entity] = new PathFollowData { pathIndex = pathPositionBuffer.Length - 1 };
            }

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
    }

    //???????????????????????? ?????????????? ?????????? ?? ???????????????????? NativeList(???????????????? ??????????)
    private static NativeList<int2> calculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            //???????? ??????
            return new NativeList<int2>(Allocator.Temp);
        }
        else
        {
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(new int2(endNode.x, endNode.y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode cameFromeNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromeNode.x, cameFromeNode.y));
                currentNode = cameFromeNode;
            }
            return path;
        }
    }

    //???????????????????????? ?????????????? ??????????, ?? ???????????????? ???? ?? ??????????
    private static void calculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPositionBuffer> pathPositionBuffer)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            //???????? ??????
        }
        else
        {
            pathPositionBuffer.Add(new PathPositionBuffer { position = new int2(endNode.x, endNode.y) });

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode cameFromeNode = pathNodeArray[currentNode.cameFromNodeIndex];
                pathPositionBuffer.Add(new PathPositionBuffer { position = new int2(cameFromeNode.x, cameFromeNode.y) });
                currentNode = cameFromeNode;
            }
        }
    }

    //?????????????????? ???? ???????? ?? ???????????????? ??????????
    private static bool isPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return gridPosition.x >= 0 &&
               gridPosition.y >= 0 &&
               gridPosition.x < gridSize.x &&
               gridPosition.y < gridSize.y;
    }

    //???????????????????????????? ?????????????? ???????????????????? ?????????????? ?? ????????????????????
    private static int calculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }

    //???????????????????????? ?????????????????? ?????????????????????? ????????
    private static int calculateDistanceCost(int2 aPosition, int2 bPosition)
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    //???????????????????? ????????????, ???????????????? ?? ?????????????? ?? ???????????????????? ????????????????????
    private static int getLowestCoatFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 0; i < openList.Length; i++)
        {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }
    private struct PathNode
    {
        public int x;
        public int y;
        public int index;
        public int gCost;
        public int hCost;
        public int fCost;
        public bool isWalkable;
        public int cameFromNodeIndex;
        public void calculateFCost()
        {
            fCost = hCost + gCost;
        }
    }
}

