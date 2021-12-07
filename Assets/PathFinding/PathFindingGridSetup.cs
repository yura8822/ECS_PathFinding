using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingGridSetup : MonoBehaviour
{
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;
    [SerializeField] private float cellSize = 10f;
    [SerializeField] Vector3 originPocitionGrid = new Vector3(125, 0, 125);


    public static PathFindingGridSetup INSTANCE { get; set; }
    public _Grid<GridNode> pathFindingGrid;

    void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Destroy(gameObject);
        }
        else
        {
            INSTANCE = this;
        }
    }
    void Start()
    {
        Vector3 offsetPositionToCenter = originPocitionGrid - new Vector3(width * cellSize * .5f, 0, height * cellSize * .5f);
        pathFindingGrid = new _Grid<GridNode>(width, height, cellSize, offsetPositionToCenter, (_Grid<GridNode> g, int x, int z) => new GridNode(g, x, z));

    }


    //UI debug obstacle
    private List<GridNode> checkList = new List<GridNode>();
    private bool isButtonPressed = false;
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isButtonPressed = true;

        }
        else if (Input.GetMouseButtonUp(1))
        {
            isButtonPressed = false;
            checkList.Clear();
        }

        if (isButtonPressed)
        {
            Vector3 position = Utils.INSTANCE.getMouseWorldPosition3D();
            GridNode gridNode = pathFindingGrid.getGridObject(position);
            if (gridNode != null && !checkList.Contains(gridNode))
            {
                gridNode.setIsWalkable(!gridNode.IsWalkable());
                checkList.Add(gridNode);
            }
        }
    }
}
