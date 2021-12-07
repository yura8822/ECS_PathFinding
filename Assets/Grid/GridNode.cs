using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode
{
    private _Grid<GridNode> grid;
    private int x;
    private int z;

    private bool isWalkable;

    public GridNode(_Grid<GridNode> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
        isWalkable = true;
    }

    public bool IsWalkable()
    {
        return isWalkable;
    }

    public void setIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
        //callback for debug
        grid.triggerGridObjectChanged(x, z);
    }

    public override string ToString()
    {
        if (isWalkable) return ".";
        else
        {
            return "O";
        }
    }
}
