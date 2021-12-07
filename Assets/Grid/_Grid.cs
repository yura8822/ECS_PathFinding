using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _Grid<TGridObject>
{
    private const bool DEBUG_UI_MODE = true;
    public event EventHandler<OnGridValueChangedEventArgs> onGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridArray;

    public _Grid(int width, int height, float cellSize, Vector3 originPosition, Func<_Grid<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }

        if (DEBUG_UI_MODE)
        {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    Vector3 centerGridPosition = offsetPositionToCenter(getWorldPosition(x, z));

                    debugTextArray[x, z] = CreateWorldText(gridArray[x, z]?.ToString(), null, centerGridPosition, 40, Color.black, TextAnchor.MiddleCenter);

                    Debug.DrawLine(getWorldPosition(x, z), getWorldPosition(x, z + 1), Color.black, 10000f);
                    Debug.DrawLine(getWorldPosition(x, z), getWorldPosition(x + 1, z), Color.black, 10000f);
                }
                Debug.DrawLine(getWorldPosition(0, height), getWorldPosition(width, height), Color.black, 10000f);
                Debug.DrawLine(getWorldPosition(width, 0), getWorldPosition(width, height), Color.black, 10000f);
            }

            onGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
            {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z].ToString();
            };
        }
    }
    private Vector3 offsetPositionToCenter(Vector3 position)
    {
        return position + new Vector3(cellSize * .5f, 0, cellSize * .5f);
    }

    public Vector3 getWorldPosition(int x, int z)
    {
        Vector3 pos = new Vector3(x, 0, z) * cellSize + originPosition;
        return pos;
    }

    public void getXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public void triggerGridObjectChanged(int x, int z)
    {
        if (onGridValueChanged != null)
        {
            onGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }
    }

    public void setGridObject(int x, int z, TGridObject value)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
            if (onGridValueChanged != null)
            {
                onGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, z = z });
            }
        }
        else
        {
            Debug.Log("GridS -> setValue(" + x + ", " + z + " ) out of bounds of gridArray, width:" + width + " height:" + height);
        }
    }

    public void setGridObject(Vector3 worldPosition, TGridObject value)
    {
        int x, z;
        getXZ(worldPosition, out x, out z);
        setGridObject(x, z, value);
    }

    public TGridObject getGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            Debug.Log("GridS -> getValue(" + x + ", " + z + " ) out of bounds of gridArray, width:" + width + " height:" + height);
            return default(TGridObject);
        }
    }

    public TGridObject getGridObject(Vector3 worldPosition)
    {
        int x, z;
        getXZ(worldPosition, out x, out z);
        return getGridObject(x, z);
    }

    public int getWidth()
    {
        return this.width;
    }

    public int getHeight()
    {
        return this.height;
    }

    public float getCellSize()
    {
        return this.cellSize;
    }

    public Vector3 getOriginPosition()
    {
        return this.originPosition;
    }
    private TextMesh CreateWorldText(string text, Transform parent, Vector3 localPosition,
                                     int fontSize, Color color, TextAnchor textAnchor)
    {
        GameObject gameObject = new GameObject("WorldText", typeof(TextMesh));
        Transform transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        transform.eulerAngles = new Vector3(90, 0, 0);
        TextMesh textMesh = gameObject.GetComponent<TextMesh>();
        textMesh.anchor = textAnchor;
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = color;
        return textMesh;
    }
}
