/*
 * The BoardGenerator to setup the board.
 */

using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    private Transform boardHolder;

    private List<Vector3> gridPositions = new List<Vector3>();
    private GameParameters GP = GameParameters.instance;

    public void SetupScene()
    {
		Debug.Log("Initialising Board generator.");
        InitialiseCamera();
        BoardSetup();
        InitialiseList();
        if (GP.randomAnchor)
        {
            AddRandomAnchorPos(GP.anchorCount);
        }
        else
        {
            SetDefaultAnchorPos();
            AddDefaultAnchorPos();
        }
        LayoutAnchors();
        SetCounterGenerator();

    }

    private void InitialiseCamera()
    {
        Camera.main.orthographicSize = GP.gridSize / 1.75f;
        Camera.main.transform.position = new Vector3(((float)(GP.gridSize / 2f - 0.5)), (float)(GP.gridSize / 2f - 0.5), -10f);
    }

    private void BoardSetup()
    {
        boardHolder = new GameObject("Board").transform;
        boardHolder.tag = "Board";

        for (int x = 0; x < GP.gridSize; x++)
        {
            for (int y = 0; y < GP.gridSize; y++)
            {
                GameObject tile = Methods.LayoutObject(GameManager.instance.gridTile, x, y);
                tile.transform.SetParent(boardHolder);
            }
        }
    }

    // gridPositions includes all position in the grid
    private void InitialiseList()
    {
        gridPositions.Clear();
        for (int x = 0; x < GP.gridSize; x++)
        {
            for (int y = 0; y < GP.gridSize; y++)
            {
                gridPositions.Add(new Vector3(x, y, 0f));
            }
        }
    }

    public bool OutOfBoundForAnchor(Vector3 position)
    {
        if (position.x < 0.5 || position.x >= GP.gridSize - 1 || position.y < 0.5 || position.y >= GP.gridSize - 1)
        {
            return true;
        }
        return false;
    }

    private void AddRandomAnchorPos(int count)
    {
        GameManager.instance.anchorPositions.Clear();
        for (int i = 0; i < count; i++)
        {
            bool valid = false;
            Vector3 randomPosition = Vector3.zero;
            while (!valid && gridPositions.Count > 0)
            {
                valid = true;
                randomPosition = Methods.RandomPosition(gridPositions);
                gridPositions.Remove(randomPosition);
                randomPosition += new Vector3(0.5f, 0.5f, 0f);
                if (OutOfBoundForAnchor(randomPosition))
                {
                    valid = false;
                    continue;
                }
                foreach (Vector3 position in GameManager.instance.anchorPositions)
                {
                    float dist = Vector3.Distance(randomPosition, position);
                    if (dist < GP.minAnchorDis)
                    {
                        valid = false;
                        break;
                    }
                }
            }
            // Avoid to add the last random position when gridPositions is empty
            if (valid)
            {
                GameManager.instance.anchorPositions.Add(randomPosition);
            }
            else
            {
                Debug.LogError("No valid space for more Anchors!");
            }
        }
    }

    private void SetDefaultAnchorPos()
    {
        if (GP.defaultAnchorPos.Count > 0) return;
        float fouth = GP.gridSize / 4 + 0.5f;
        GP.defaultAnchorPos.Add(new Vector3(GP.gridSize - fouth, fouth, 0f));
        GP.defaultAnchorPos.Add(new Vector3(fouth, GP.gridSize - fouth, 0f));
        GP.defaultAnchorPos.Add(new Vector3(GP.gridSize - fouth, GP.gridSize - fouth, 0f));
        GP.defaultAnchorPos.Add(new Vector3(fouth, fouth, 0f));
    }

    private void AddDefaultAnchorPos()
    {
        foreach (Vector3 pos in GP.defaultAnchorPos)
        {
            GameManager.instance.anchorPositions.Add(pos);
        }
    }

    private void LayoutAnchors()
    {
        foreach (Vector3 pos in GameManager.instance.anchorPositions)
        {
            Methods.LayoutObject(GameManager.instance.Anchor, pos.x, pos.y);
            Methods.LayoutObject(GameManager.instance.OnAnchor, pos.x, pos.y);
        }
    }

    private void SetCounterGenerator()
    {
        GameManager.instance.generators.Clear();
        GameManager.instance.parkingPos.Clear();
        GameManager.instance.generators.Add(Methods.LayoutObject(GameManager.instance.GeneratorsImages[0], -1.5f, GP.gridSize - 2f));
        GameManager.instance.parkingPos.Add(new Vector3(-1.5f - 2.2f, GP.gridSize - 2f - 2f, 0f));
        GameManager.instance.generators.Add(Methods.LayoutObject(GameManager.instance.GeneratorsImages[1], -1.5f, 2f));
        GameManager.instance.parkingPos.Add(new Vector3(-1.5f - 2.2f, 2f + 1.5f, 0f));
        GameManager.instance.generators.Add(Methods.LayoutObject(GameManager.instance.GeneratorsImages[2], GP.gridSize + 0.5f, GP.gridSize - 2f));
        GameManager.instance.parkingPos.Add(new Vector3(GP.gridSize + 0.5f + 2.2f, GP.gridSize - 2f - 2f, 0f));
        GameManager.instance.generators.Add(Methods.LayoutObject(GameManager.instance.GeneratorsImages[3], GP.gridSize + 0.5f, 2f));
        GameManager.instance.parkingPos.Add(new Vector3(GP.gridSize + 0.5f + 2.2f, 2f + 1.5f, 0f));
    }
}
