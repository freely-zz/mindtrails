/*
 * This class contains several general helper methods and algorithms that may help you develop your agent 
 * plus various system tools.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Methods : MonoBehaviour
{
    //for path-planning
    private static List<List<Vector3>> path = new List<List<Vector3>>();
    private static List<List<bool>> visited = new List<List<bool>>();
    private static int[] dx = {  -1, 1, 0, 0, -1, 1, -1, 1 };
    private static int[] dy = {  0, 0, -1, 1, -1, 1, 1, -1 };
    //for managing gameobjects
    private static List<GameObject> waitToDestoryCounter = new List<GameObject>();
    private static List<GameObject> waitToActiveCounter = new List<GameObject>();
    private static List<GameObject> shuttles = new List<GameObject>();
    //for tile color
    private static Vector4 tileColor = new Vector4(139/255f, 159/255f, 130/255f, 0.2f);

    
    //=======================================================================================
    //   *********************************HELPER METHODS*********************************
    //=======================================================================================

    //Highlight cells
    public static void HighlightCells(List<Vector3> cellList)
    {
        GameObject[] gridtiles = GameObject.FindGameObjectsWithTag("Floor");
        
        foreach (Vector3 cell in cellList)
        {
            if (IsOnBoard(cell))
            {
                foreach(GameObject gt in gridtiles)
                {
                    if (cell == gt.transform.position)
                    {
                        //gt.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
                        gt.GetComponent<Renderer>().material.SetColor("_Color", new Color(0f,1f,0f,0.5f));
                    }
                }
            }
        }

    }

    public static void UnhighlightCells()
    {
        GameObject[] gridtiles = GameObject.FindGameObjectsWithTag("Floor");

                foreach (GameObject gt in gridtiles)
                {
                     gt.GetComponent<Renderer>().material.SetColor("_Color", new Color(181/255f,171/255f,171/255f,0.5f));
                }
    }

    //Returns how many reds in specified generator
    public static int RedsInGen(int genNum)
    {
        GameObject generator = GameManager.instance.generators[genNum];
        int reds = 0;
        foreach(GameObject counter in generator.GetComponent<GeneratorManager>().GetPickupsInGn())
        {
            if (generator.GetComponent<GeneratorManager>().GetPickupsInGnColor(counter.transform.position) == 0)
            {
                reds += 1;
            }
        }
        return reds;
    }

    //Returns list of colours in specified generator
    public static List<int> ColorsInGen(int genNum)
    {
        GameObject generator = GameManager.instance.generators[genNum];
        return generator.GetComponent<GeneratorManager>().GetColorList();
    }

    //Check color of counter at this index
    public static bool IsRed(int genNum, int index)
    {
        GameObject generator = GameManager.instance.generators[genNum];
        return generator.GetComponent<GeneratorManager>().GetColorList()[index]==Constants.RED;
    }


    // Returns list of counters currently available from each generator
    public static List<List<int>> CountersAvailable()
    {
        List<List<int>> colorList = new List<List<int>>();
        foreach (GameObject g in GameManager.instance.generators)
        {
            List<int> colorsInGn = g.GetComponent<GeneratorManager>().GetColorList();
            colorList.Add(colorsInGn);
        }

        return colorList;
    }

    // Returns the generator which has the most number of red pickups, if no red counter in any generator, return the first generator
    public static int MostRedGenerator()
    {
        int mostCount = -1;
        int bestGenerator = 0;
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            int nowCount = GameManager.instance.generators[i].GetComponent<GeneratorManager>().GetRedPickupsNumber();
            if (nowCount > mostCount)
            {
                mostCount = nowCount;
                bestGenerator = i;
            }
        }
        return bestGenerator;
    }

    // Overloads above. Returns the generator which has the most number of red pickups excluding nominated generator.
    public static int MostRedGenerator(int exclude)
    {
        int mostCount = -1;
        int bestGenerator = 0;
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            if (i == exclude) continue;
            int nowCount = GameManager.instance.generators[i].GetComponent<GeneratorManager>().GetRedPickupsNumber();
            if (nowCount > mostCount)
            {
                mostCount = nowCount;
                bestGenerator = i;
            }
        }
        return bestGenerator;
    }

    // Returns random Anchor pos excluding pos in list note centre of anchor is BETWEEN tiles 
    // i.e., anchor at (10.5,10.5) fills grid locations (10,10), (10,11), (11,10) and (11,11).
    public static Vector3 RandomAnchor(List<Vector3> list)
    {
        int index = Random.Range(0, GameManager.instance.anchorPositions.Count);
        while (list.Contains(GameManager.instance.anchorPositions[index]))
        {
            index = Random.Range(0, GameManager.instance.anchorPositions.Count);
        }
        return GameManager.instance.anchorPositions[index];
    }

    // Returns Anchor with the shortest linear distance from pos, excluding those in list
    public static Vector3 SearchClosestAnchor(Vector3 pos, List<Vector3> list)
    {
        Vector3 anchor = Vector3.zero;
        float dist = Mathf.Infinity;
        foreach (Vector3 position in GameManager.instance.anchorPositions)
        {
            if (Vector3.Distance(pos, position) < dist && !list.Contains(position))
            {
                dist = Vector3.Distance(pos, position);
                anchor = position;
            }
        }
        return anchor;
    }

    //return list of actual gridlocations covered by anchorpos; or empty list if no such anchor
    public static List<Vector3> GetAnchorGridLocs(Vector3 anchorPos)
    {
        if (!GameManager.instance.anchorPositions.Contains(anchorPos)) return new List<Vector3>();

        List<Vector3> gridLocations = new List<Vector3>();
        gridLocations.Add(new Vector3(Mathf.FloorToInt(anchorPos.x), Mathf.FloorToInt(anchorPos.y), 0));
        gridLocations.Add(new Vector3(Mathf.FloorToInt(anchorPos.x), Mathf.CeilToInt(anchorPos.y), 0));
        gridLocations.Add(new Vector3(Mathf.CeilToInt(anchorPos.x), Mathf.FloorToInt(anchorPos.y), 0));
        gridLocations.Add(new Vector3(Mathf.CeilToInt(anchorPos.x), Mathf.CeilToInt(anchorPos.y), 0));
        return gridLocations;
    }

    //If the pos is on anchor, return the position of the anchor's center, else return Vector3.zero
    public static Vector3 GetAnchorCenter(Vector3 pos)
    {
        foreach (Vector3 position in GameManager.instance.anchorPositions)
        {
            if (Vector3.Distance(position, pos) < 1f)
            {
                return position;
            }
        }
        return Vector3.zero;
    }

    //Return list of anchors
    public static List<Vector3> GetAllAnchors()
    {
        return GameManager.instance.anchorPositions;
    }

    // Checks if the position is valid on the board
    public static bool IsOnBoard(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < GameParameters.instance.gridSize && pos.y >= 0 && pos.y < GameParameters.instance.gridSize)
        {
            return true;
        }
        return false;
    }

    // Checks if the position is empty grid
    public static bool IsEmptyGrid(Vector3 pos)
    {
        if (GameManager.instance.deposited[(int)pos.x][(int)pos.y] == -1 && GetAnchorCenter(pos) == Vector3.zero && TileExist(pos))
        {
            return true;
        }
        return false;
    }

    // Checks if two positions are adjacent
    public static bool IsAdjGrid(Vector3 pos1, Vector3 pos2)
    {
        if (Mathf.Approximately(Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y), 1f))
        {
            return true;
        }
        return false;
    }

    
    // Returns colour of counter at pos; or -1 if empty grid or off board.
    public static int OnCounter(Vector3 pos)
    {
        if (IsOnBoard(pos))
        {
            int col = GameManager.instance.deposited[(int)pos.x][(int)pos.y];
            if (col > 10)
            {
                col -= 10;
            }
            return col;
        }
        return -1;
    }

    //get list of random empty grid locations
    public static List<Vector3> GetRandomEmptyGrid(int num)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < num; i++)
        {
            int x = Random.Range(0, Config.gridSize);
            int y = Random.Range(0, Config.gridSize);
            while (!IsEmptyGrid(new Vector3(x, y, 0f)) || positions.Contains(new Vector3(x, y, 0f)))
            {
                x = Random.Range(0, Config.gridSize);
                y = Random.Range(0, Config.gridSize);
            }
            positions.Add(new Vector3(x, y, 0f));
        }

        return positions;
    }

    //get list of random empty grid locations within region
    public static List<Vector3> GetRandomEmptyGrid(int num, Vector3 cornerA, Vector3 cornerB)
    {
        List<Vector3> positions = new List<Vector3>();
        int x, y;
        for (int i = 0; i < num; i++)
        {
            x = Random.Range(Mathf.Min(Mathf.FloorToInt(cornerA.x), Mathf.FloorToInt(cornerB.x)), Mathf.Max(Mathf.CeilToInt(cornerA.x), Mathf.CeilToInt(cornerB.x))+1);
            y = Random.Range(Mathf.Min(Mathf.FloorToInt(cornerA.y), Mathf.FloorToInt(cornerB.y)), Mathf.Max(Mathf.CeilToInt(cornerA.y), Mathf.CeilToInt(cornerB.y)) + 1);

            while (!IsEmptyGrid(new Vector3(x, y, 0f)) || positions.Contains(new Vector3(x, y, 0f)))
            {
                x = Random.Range(Mathf.Min(Mathf.FloorToInt(cornerA.x), Mathf.FloorToInt(cornerB.x)), Mathf.Max(Mathf.CeilToInt(cornerA.x), Mathf.CeilToInt(cornerB.x)) + 1);
                y = Random.Range(Mathf.Min(Mathf.FloorToInt(cornerA.y), Mathf.FloorToInt(cornerB.y)), Mathf.Max(Mathf.CeilToInt(cornerA.y), Mathf.CeilToInt(cornerB.y)) + 1);
            }
            positions.Add(new Vector3(x, y, 0f));
        }

        return positions;
    }

    //check if pos is in the narrative region between anchors
    public static bool IsInner(Vector3 pos, List<Vector3> anchors)
    {
        int minX = Mathf.Min (Mathf.FloorToInt(anchors[0].x), Mathf.FloorToInt(anchors[1].x));
        int maxX = Mathf.Max (Mathf.CeilToInt(anchors[0].x), Mathf.CeilToInt(anchors[1].x));
        int minY = Mathf.Min (Mathf.FloorToInt(anchors[0].y), Mathf.FloorToInt(anchors[1].y));
        int maxY = Mathf.Max (Mathf.CeilToInt(anchors[0].y), Mathf.CeilToInt(anchors[1].y));

        return (pos.x >= minX) && (pos.x <= maxX) && (pos.y >= minY) && (pos.y <= maxY);
    }



    //get distance of point from narrative region between anchors
    public static int FarFrom(Vector3 pos, List<Vector3> anchors)
    {
        int minX = Mathf.Min(Mathf.FloorToInt(anchors[0].x), Mathf.FloorToInt(anchors[1].x));
        int maxX = Mathf.Max(Mathf.CeilToInt(anchors[0].x), Mathf.CeilToInt(anchors[1].x));
        int minY = Mathf.Min(Mathf.FloorToInt(anchors[0].y), Mathf.FloorToInt(anchors[1].y));
        int maxY = Mathf.Max(Mathf.CeilToInt(anchors[0].y), Mathf.CeilToInt(anchors[1].y));

        float width = maxX - minX;
        float height = maxY - minY;
        Vector2 centrePoint = new Vector2(minX+width / 2f, minY+height / 2f);

        float difx = Mathf.Max(Mathf.Abs(pos.x - centrePoint.x) - width / 2, 0);
        float dify = Mathf.Max(Mathf.Abs(pos.y - centrePoint.y) - height / 2, 0);
        return Mathf.CeilToInt(Mathf.Sqrt(difx * difx + dify * dify));
    }

    // Returns a random position from the list
    public static Vector3 RandomPosition(List<Vector3> list)
    {
        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }

    //Returns randomised list of num integers (to randomise indices)
    public static List<int> RandomOrder(int num)
    {
        List<int> index = new List<int>();
        for (int i = 0; i < num; i++)
        {
            index.Add(i);
        }
        List<int> randomOrder = new List<int>();
        while (index.Count > 0)
        {
            int i = Random.Range(0, index.Count);
            randomOrder.Add(index[i]);
            index.RemoveAt(i);
        }
        return randomOrder;
    }



    //=============================================================================================
    //RUDIMENTARY PATH-PLANNING - INCLUDES ROUTINE FOR CHECKING GAMEOVER
    //=============================================================================================
    // Returns a path in grid from start to end
    // if onlyRedCounter == true, the path only throughs red counters
    public static List<Vector3> FindPathInGrid(Vector3 start, Vector3 end, bool onlyRedCounter)
    {
        InitialisePath();
        path[(int)start.x][(int)start.y] = new Vector3(-1, -1, 0f);   //Unvisited positions are -2
        Queue queue = new Queue();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector3 now = (Vector3)queue.Dequeue();
            for (int i = 0; i < GameParameters.instance.searchDirections; i++)//neighbours
            {
                Vector3 pos = new Vector3(now.x + dx[i], now.y + dy[i], 0f);
                if (IsOnBoard(pos) && TileExist(pos) && path[(int)pos.x][(int)pos.y].x < -1)
                {
                    if (!onlyRedCounter || GetAnchorCenter(pos) != Vector3.zero || GameManager.instance.deposited[(int)pos.x][(int)pos.y] == -1 || GameManager.instance.deposited[(int)pos.x][(int)pos.y] == 0)
                    {
                        queue.Enqueue(pos);
                        path[(int)pos.x][(int)pos.y] = now;
                        if (Mathf.Abs(pos.x - end.x) < 1f && Mathf.Abs(pos.y - end.y) < 1f)
                        {
                            return FindPath(pos);
                        }
                    }
                }
            }
        }
        return new List<Vector3>();
    }

    //checks if possible to reach another anchor from this pos through reds only
    public static bool BFStoAnotherAnchor(Vector3 start)
    {
        Vector3 startAnchorCenter = GetAnchorCenter(start);
        InitialiseVisited();
        Queue queue = new Queue();
        queue.Enqueue(start);
        visited[(int)start.x][(int)start.y] = true;
        while (queue.Count > 0)
        {
            Vector3 now = (Vector3)queue.Dequeue();
            for (int i = 0; i < GameParameters.instance.searchDirections; i++)
            {
                Vector3 pos = new Vector3(now.x + dx[i], now.y + dy[i], 0f);
                if (IsOnBoard(pos))
                {
                    if (!visited[(int)pos.x][(int)pos.y] && (GameManager.instance.deposited[(int)pos.x][(int)pos.y] == 0 || GetAnchorCenter(pos) != Vector3.zero))
                    {
                        queue.Enqueue(pos);
                        visited[(int)pos.x][(int)pos.y] = true;
                        if (GetAnchorCenter(pos) != Vector3.zero && GetAnchorCenter(pos) != startAnchorCenter)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // Finds num neighbors around list, return empty list if it is hard to find
    public static List<Vector3> FindEmptyNeighbor(List<Vector3> list, int num, List<Vector3> addedToDeposit, int neighborRange)
    {
        List<Vector3> neighbor = new List<Vector3>();
        int randomCount = 0;
        while (num > 0)
        {
            Vector3 pos = list[Random.Range(0, list.Count)];
            Vector3 newNeighbor = new Vector3(pos.x + Random.Range(0, neighborRange), pos.y + Random.Range(0, neighborRange), 0f);
            if (IsEmptyGrid(newNeighbor) && !addedToDeposit.Contains(newNeighbor) && !list.Contains(newNeighbor) && !neighbor.Contains(newNeighbor))
            {
                neighbor.Add(newNeighbor);
                num--;
            }
            // Hard to find neighbor in range
            randomCount++;
            if (randomCount > 50)
            {
                return new List<Vector3>();
            }
        }
        return neighbor;
    }

    private static void InitialisePath()
    {
        path.Clear();
        for (int x = 0; x < GameParameters.instance.gridSize; x++)
        {
            path.Add(new List<Vector3>());
            for (int y = 0; y < GameParameters.instance.gridSize; y++)
            {
                path[x].Add(new Vector3(-2, -2, 0f));
            }
        }
    }

    private static void InitialiseVisited()
    {
        visited.Clear();
        for (int x = 0; x < GameParameters.instance.gridSize; x++)
        {
            visited.Add(new List<bool>());
            for (int y = 0; y < GameParameters.instance.gridSize; y++)
            {
                visited[x].Add(false);
            }
        }
    }

    private static List<Vector3> FindPath(Vector3 pos)
    {
        List<Vector3> pathList = new List<Vector3>();
        pathList.Clear();
        while (path[(int)pos.x][(int)pos.y].x >= 0)
        {
            pathList.Add(pos);
            pos = path[(int)pos.x][(int)pos.y];
        }
        pathList.Reverse();
        return pathList;
    }

    public static int StepsRemaining(List<Vector3> anchors)
    {
        //stub.
        return 3;
    }

    //=================================================================================
    //MOSTLY SYSTEM METHODS
    //=================================================================================
    public static void InitializeMethods()
    {
        waitToDestoryCounter.Clear();
        waitToActiveCounter.Clear();
        shuttles.Clear();
    }


    public static IEnumerator TurnWhiteCounterOver(Vector3 pos, float turnOverDelay, GameObject shuttle)
    {
        GameObject[] counters;
        counters = GameObject.FindGameObjectsWithTag("WhiteCounter");
        foreach (GameObject counter in counters)
        {
            if (counter != null && counter.transform.position == pos)
            {
                counter.SetActive(false);
                if (OnCounter(pos) == -1) continue;
                GameObject colorCounter = LayoutObject(GameManager.instance.counterTiles[OnCounter(pos)], pos.x, pos.y);
                GameManager.instance.convincers.Add(pos);//maintain list to pass to PlayerB
                yield return new WaitForSeconds(turnOverDelay);
                // Do not turn back if game over or keep collision with shuttle
                if (!GameManager.instance.gameOver && !colorCounter.GetComponent<BoxCollider2D>().IsTouching(shuttle.GetComponent<BoxCollider2D>()))
                {
                    Destroy(colorCounter);
                    counter.SetActive(true);
                }
                else
                {
                    waitToDestoryCounter.Add(colorCounter);
                    waitToActiveCounter.Add(counter);
                    shuttles.Add(shuttle);
                }
            }
        }
        
    }



    // Checks if on parking position (to collect from generator)
    public static bool IsOnPark(Vector3 pos)
    {
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            if (GameManager.instance.parkingPos[i].x == pos.x && GameManager.instance.parkingPos[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }

    // Returns the generator ID corresponding to the parking position
    public static int FindGenerator(Vector3 pos)
    {
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            if (GameManager.instance.parkingPos[i] == pos)
            {
                return i;
            }
        }
        return -1;
    }

    // Checks if the position belongs to the generator
    public static bool IsInGn(Vector3 pos, int generatorId)
    {
        GameObject generator = GameManager.instance.generators[generatorId];
        foreach (GameObject pickups in generator.GetComponent<GeneratorManager>().GetPickupsInGn())
        {
            if (pickups.transform.position == pos)
            {
                return true;
            }
        }
        return false;
    }

    // Returns the generator ID if the pos has a pickup
    public static int OnPickup(Vector3 pos)
    {
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            List<GameObject> pickups = GameManager.instance.generators[i].GetComponent<GeneratorManager>().GetPickupsInGn();
            for (int j = 0; j < pickups.Count; j++)
            {
                if (pickups[j].transform.position == pos)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public static void TurnAllWhiteCounterOver()
    {
        GameObject[] counters;
        counters = GameObject.FindGameObjectsWithTag("WhiteCounter");
        foreach (GameObject counter in counters)
        {
            if (OnCounter(counter.transform.position) != -1)
            {
                LayoutObject(GameManager.instance.counterTiles[OnCounter(counter.transform.position)], counter.transform.position.x, counter.transform.position.y);
            }
            Destroy(counter);
        }
    }

    // Checks the position hasn't been blocked
    public static bool TileExist(Vector3 pos)
    {
        return !GameManager.instance.blocked[(int)pos.x][(int)pos.y];
    }

    public static void BlockTile(Vector3 pos)
    {
        GameManager.instance.blocked[(int)pos.x][(int)pos.y] = true;
    }

    // Randomly chooses from yellow and blue
    public static int RandomCarryCounter(int[] carry)
    {
        List<int> bag = new List<int>();
        for (int i = 1; i < 3; i ++)
        {
            for (int j = 0; j < carry[i]; j++)
            {
                bag.Add(i);
            }
        }
        return bag[Random.Range(0, bag.Count)];
    }


    // Returns how many red, yellow and blue counters can be picked up if move according to list, carryLimit is n
    public static int[] PickupColorInPos(List<Vector3> list, int n)
    {
        int[] carry = new int[3];
        foreach (Vector3 pos in list)
        {
            if (carry.Sum() == n) break;
            int id = OnPickup(pos);
            if (id > -1)
            {
                carry[GameManager.instance.generators[id].GetComponent<GeneratorManager>().GetPickupsInGnColor(pos)]++;
            }
        }
        return carry;
    }


    // Transfer anchor's center position to a valid grid
    public static Vector3 TransAnchorPositionInGrid(Vector3 position)
    {
        return new Vector3(position.x - 0.5f, position.y - 0.5f, 0f);
    }

    // Returns num pickups' positions in the generator
    public static List<Vector3> PickupsPosInGn(int generatorId, int num)
    {
        List<Vector3> pickupsPos = new List<Vector3>();
        List<GameObject> pickups = GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().GetPickupsInGn();
        for (int i = 0; i < pickups.Count; i++)
        {
            //if (generator.GetComponent<GeneratorManager>().GetPickupsInGnColor(pickups[i].transform.position) == 0)
            pickupsPos.Add(pickups[i].transform.position);
            num--;
            if (num == 0) break;
        }
        return pickupsPos;
    }

    public static bool OnParkingPos(Vector3 pos)
    {
        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            if (GameManager.instance.parkingPos[i] == pos)
            {
                return true;
            }
        }
        return false;
    }

    // Removes all deposited grids and anchors in the given list
    public static List<Vector3> RemoveDepositedAndAnchor(List<Vector3> list)
    {
        List<Vector3> validList = new List<Vector3>();
        for (int i = 0; i < list.Count; i++)
        {
            if (IsEmptyGrid(list[i]))
            {
                validList.Add(list[i]);
            }
        }
        return validList;
    }

    private static int FindChainCost(Vector3 start, Vector3 end, bool onlyRed)
    {
        List<Vector3> path = FindPathInGrid(start, end, onlyRed);
        List<Vector3> emptyPos = RemoveDepositedAndAnchor(path);
        return emptyPos.Count;
    }

    public static bool ReadyToTurnOver(Vector3 pos)
    {
        if (IsOnBoard(pos))
        {
            return GameManager.instance.readyToTurnOver[(int)pos.x][(int)pos.y];
        }
        return false;
    }


    public static void SetReadyToTurnOver(Vector3 pos, bool ready)
    {
        if (IsOnBoard(pos))
        {
            GameManager.instance.readyToTurnOver[(int)pos.x][(int)pos.y] = ready;
        }
    }

    public static void IncConvincers(Vector3 pos)
    {
        GameManager.instance.convincers.Add(pos);//maintain list to pass to PlayerB
    }


    public static GameObject LayoutObject(GameObject prefab, float x, float y)
    {
        Vector3 position = new Vector3(x, y, 0f);
        return Instantiate(prefab, position, Quaternion.identity);
    }

    public static void DestroyCounter(GameObject obj, float x, float y)
    {
        waitToDestoryCounter.Add(obj);
    }


    private void FixedUpdate()
    //TODO: move these tile-related methods
    {
        if (waitToDestoryCounter.Count == 0) return;
        if (!GameManager.instance.gameOver && !waitToDestoryCounter[0].GetComponent<BoxCollider2D>().IsTouching(shuttles[0].GetComponent<BoxCollider2D>()))
        {
            Destroy(waitToDestoryCounter[0]);
            if (GameManager.instance.deposited[(int)waitToActiveCounter[0].transform.position.x][(int)waitToActiveCounter[0].transform.position.y] == -1)
            {
                Destroy(waitToActiveCounter[0]);
            }
            else
            {
                waitToActiveCounter[0].SetActive(true);
            }
            waitToDestoryCounter.RemoveAt(0);
            waitToActiveCounter.RemoveAt(0);
            shuttles.RemoveAt(0);
        }
    }

}
