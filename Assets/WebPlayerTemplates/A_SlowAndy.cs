/*
 * Tests out revised path-planner.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

public class A_SlowAndy : IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;
    private int shuttles = Config.shuttleNum;
    private List<Vector3> trueAnchors = new List<Vector3>();
    private List<Vector3> plannedPath = new List<Vector3>();
    private List<Vector3> trueGrid = new List<Vector3>();

    //shuttleZone refers to a zone where the shuttle wants to go to reveal fake colours
    private List<Vector3> shuttleZone = new List<Vector3>();
    private Vector3 shuttleZoneCent;
    private int shuttleZoneSize;

    private int Tpathindex = 0;
    private int Fpathindex = 0;
    private float limiter = 0;
    private int redCount = 0;

    private List<Vector3> fakePath = new List<Vector3>();


    //private float selfScore = 0;

    public List<Vector3> RegisterTrueAnchors()
    {
        //choose the 2 anchors thats most in the center.
        //TODO fake anchor shouldn't be random.
        trueAnchors.Add(getCentralAnchor(new List<Vector3>()));
        trueAnchors.Add(getCentralAnchor(trueAnchors));
        //trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[0], trueAnchors));

        //calculate a fake zone.
        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueGrid = selectZone(trueAnchors[0], trueAnchors[1]);
        CalcShuttleZone();
        Methods.HighlightCells(shuttleZone);

        return trueAnchors;
    }


    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Tpathindex = 0;
        Fpathindex = 0;
        limiter = 0;

        Vector3 minBlock = CalcScore(blocks, 0);
        /*List<Actions> actionsList = new List<Actions>();

        for (int i = 0; i < shuttles; i++)
        {
            actionsList.Add(new Actions())
        }*/
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        // float randomness = 0.7f;

        //generate shortest path - may include reds (automatically constrained by diags setting)
        plannedPath = PathFinder.GetPath(trueAnchors[0], trueAnchors[1]);
        plannedPath = PathFinder.StripPath(plannedPath);

        fakePath = PathFinder.GetPath(trueAnchors[0], trueAnchors[2]);
        fakePath = PathFinder.StripPath(fakePath);

        Methods.UnhighlightCells();
        Methods.HighlightCells(plannedPath);
        //Methods.HighlightCells(fakePath);

        if (plannedPath.Count > 0)
        {

            //shuffle the path
            plannedPath = Shuffle(plannedPath);
            fakePath = Shuffle(fakePath);

            //set speed to high
            actions1.Speed(5f);
            actions2.Speed(5f);

            //collect from generators with most reds
            int gridnum1 = Methods.MostRedGenerator();
            int gridnum2 = Methods.MostRedGenerator(gridnum1);

            actions1.CollectFromGen(gridnum1, carryLimit);
            actions2.CollectFromGen(gridnum2, carryLimit);

            int reds1 = Methods.RedsInGen(gridnum1);
            int reds2 = Methods.RedsInGen(gridnum2);

            redCount = reds1 + reds2;
            //List<int> colors1 = Methods.ColorsInGen(gridnum1);
            //List<int> colors2 = Methods.ColorsInGen(gridnum1);

            //deposit tokens
            for (int i = 0; i < carryLimit; i++)
            {
                depositLogic(gridnum1, actions1, plannedPath, fakePath, i, minBlock);
                depositLogic(gridnum2, actions2, plannedPath, fakePath, i, minBlock);
            }

            //TODO: figure out how to avoid true path zone.
            actions1.MoveTo(shuttleZoneCent);
            actions2.MoveTo(shuttleZoneCent);

            actions1.Park();
            actions2.Park();
        }

        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
    }

    //calculate score based on euclidean distance between blocks and real anchor.
    public Vector3 CalcScore(List<Vector3> blocks, float score)
    {
        float dx, dy;
        float distance1, distance2, distance3;

        Vector3 minBlock = Vector3.zero;
        float minScore = Mathf.NegativeInfinity;

        foreach (var b in blocks)
        {
            dx = b[0] - trueAnchors[0][0];
            dy = b[1] - trueAnchors[0][1];
            distance1 = Mathf.Sqrt(dx * dx + dy * dy);

            dx = b[0] - trueAnchors[1][0];
            dy = b[1] - trueAnchors[1][1];
            distance2 = Mathf.Sqrt(dx * dx + dy * dy);

            dx = b[0] - trueAnchors[2][0];
            dy = b[1] - trueAnchors[2][1];
            distance3 = Mathf.Sqrt(dx * dx + dy * dy);
            //UnityEngine.Debug.Log(distance2);
            //UnityEngine.Debug.Log(distance3);
            UnityEngine.Debug.Log(distance2 - distance3);
            score += distance2 - distance3;

            if (distance2 - distance3 > minScore)
            {
                minBlock = b;
                minScore = distance2 - distance3;
            }

        }

        return minBlock;
    }

    //place block near location block. variance is how far away it could be.
    public Vector3 randNearBlock(Vector3 block, int variance = 0)
    {
        if (variance == 0)
        {
            variance = gridSize / 5;
        }

        //random variance and snap to inside grid
        int dx = Random.Range(-variance, variance);
        int dy = Random.Range(-variance, variance);

        float blockx = dx + block[0];
        blockx = Mathf.Max(blockx, 0f);
        blockx = Mathf.Min(blockx, gridSize - 1);

        float blocky = dy + block[1];
        blocky = Mathf.Max(blocky, 0f);
        blocky = Mathf.Min(blocky, gridSize - 1);

        if (Methods.IsEmptyGrid(new Vector3(blockx, blocky, 0)))
        {
            return new Vector3(blockx, blocky, 0);
        }
        else
        {
            return Vector3.zero;
        }
    }

    //https://answers.unity.com/questions/486626/how-can-i-shuffle-alist.html
    //Shuffles a list to randomize its order
    public List<T> Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            //swap items in list randomly
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }

    //return a id which correspond to a block location, if no location found within j interations return -1.
    public int randShutZoneInd()
    {
        if (shuttleZone.Count < 1)
        {
            return -1;
        }

        int i = Random.Range(0, shuttleZone.Count - 1);
        int j = 0;

        //while the grid is not empty, we keep looking for empty grid.
        while (!Methods.IsEmptyGrid(shuttleZone[i]) && j < 5)
        {
            i = Random.Range(0, shuttleZone.Count - 1);
            j++;
        }

        if (j >= 5)
        {
            return -1;
        }

        return i;
    }

    //logic for depositing blocks.
    public void depositLogic(int gridnum1, Actions actions1, List<Vector3> plannedPath, List<Vector3> fakePath, int i, Vector3 minBlock)
    {
        float e = Random.Range(0.0f, 1.0f);
        //token is red
        if (Methods.IsRed(gridnum1, i))
        {
            //chance to trigger, check if random location on fake path is empty.
            if (e < (0.3 + redCount / (shuttles * 4) * limiter) && Fpathindex < fakePath.Count && Methods.IsEmptyGrid(fakePath[Fpathindex]))
            {
                actions1.DepositAt(fakePath[Fpathindex]);
                Fpathindex++;
                //limiter -= 0.5f;
            }
            //chance to trigger, try to place block in fake zone
            else if (e < 0.5 + redCount / (shuttles * 4) * limiter)
            {
                int rand = randShutZoneInd();
                if (rand <= -1)
                {
                    actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                }
                actions1.DepositAt(shuttleZone[rand]);
            }
            //else try to put on true path
            else if (Tpathindex < plannedPath.Count && Methods.IsEmptyGrid(plannedPath[Tpathindex]))
            {
                actions1.DepositAt(plannedPath[Tpathindex]);
                Tpathindex++;
                limiter++;
            }
            //else put randomly
            else
            {
                actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
            }
        }
        //yellow or blue
        else
        {
            float e2 = Random.Range(0.0f, 1.0f);
            //there exist a block with minimum score.
            if (minBlock != Vector3.zero)
            {
                if (e < 0.8)
                {
                    //put token near that block, position variance is default 2.
                    placeRand(actions1, minBlock);
                }
                //place in shuttle zone
                else
                {
                    int rand = randShutZoneInd();
                    if (rand <= -1)
                    {
                        actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                    else
                    {
                        actions1.DepositAt(shuttleZone[rand]);
                    }
                }

            }
            //exist block.
            else
            {
                //place block in shuttle zone.
                if (e < 0.1)
                {
                    int rand = randShutZoneInd();
                    if (rand <= -1)
                    {
                        actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                    else
                    {
                        actions1.DepositAt(shuttleZone[rand]);
                    }
                }
                // place near true path
                else if (e < 0.55)
                {
                    placeRand(actions1, plannedPath[Tpathindex]);
                }
                // place near fake path
                else
                {
                    placeRand(actions1, fakePath[Fpathindex]);
                }
            }
        }
    }

    //attempts to place near arg block using randNearBlock function. if fail, place randomly
    public void placeRand(Actions action, Vector3 block)
    {
        Vector3 randBlock = randNearBlock(block);
        if (randBlock != Vector3.zero)
        {
            action.DepositAt(randBlock);
        }
        else
        {
            action.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
        }
    }

    //select a rectangular zone between p1 and p2.
    public List<Vector3> selectZone(Vector3 p1, Vector3 p2)
    {
        List<Vector3> rectangle = new List<Vector3>();

        int minX = Mathf.Min(Mathf.FloorToInt(p1.x), Mathf.FloorToInt(p2.x));
        int maxX = Mathf.Max(Mathf.CeilToInt(p1.x), Mathf.CeilToInt(p2.x));
        int minY = Mathf.Min(Mathf.FloorToInt(p1.y), Mathf.FloorToInt(p2.y));
        int maxY = Mathf.Max(Mathf.CeilToInt(p1.y), Mathf.CeilToInt(p2.y));

        for (int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                rectangle.Add(new Vector3((float)i, (float)j, 0f));
            }
        }

        return rectangle;
    }

    //get a fake zone which encourages shuttles to go through.
    public void CalcShuttleZone()
    {
        int maxX, minX, maxY, minY;
        Vector3 p1 = trueAnchors[0];
        Vector3 p2 = trueAnchors[1];
        int dx = (int)(p1[0] - p2[0]);
        int dy = (int)(p1[1] - p2[1]);

        //find the coordinates for fake zone;
        minX = (int)Mathf.Min(p1[0] - 0.5f, p1[0] - 0.5f + dx);
        minX = Mathf.Max(minX, 0);

        maxX = (int)Mathf.Max(p1[0] + 0.5f, p1[0] + 0.5f + dx);
        maxX = Mathf.Min(gridSize - 1, maxX);

        minY = (int)Mathf.Min(p1[1] - 0.5f, p1[1] - 0.5f + dy);
        minY = Mathf.Max(minY, 0);

        maxY = (int)Mathf.Max(p1[1] + 0.5f, p1[1] + dy + 0.5f);
        maxY = Mathf.Min(gridSize - 1, maxY);

        for (int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                shuttleZone.Add(new Vector3((float)i, (float)j, 0f));
            }
        }

        //remove anchors from zone.
        shuttleZone.Remove(new Vector3(p1[0] + 0.5f, p1[1] + 0.5f, 0));
        shuttleZone.Remove(new Vector3(p1[0] - 0.5f, p1[1] + 0.5f, 0));
        shuttleZone.Remove(new Vector3(p1[0] + 0.5f, p1[1] - 0.5f, 0));
        shuttleZone.Remove(new Vector3(p1[0] - 0.5f, p1[1] - 0.5f, 0));

        shuttleZoneCent = Vector3.Lerp(new Vector3(minX, maxY, 0), new Vector3(maxX, minY, 0), 0.5f);
        shuttleZoneSize = shuttleZone.Count() - 4;
    }

    //calculate how centered each anchor is based on distance to border.
    public float calcCentrality(Vector3 Anchor)
    {
        float x, y;
        float center = gridSize / 2;
        x = Mathf.Abs(Anchor[0] - center);
        y = Mathf.Abs(Anchor[1] - center);
        return x + y;
    }

    //get the most center anchor excluding exlude list.
    public Vector3 getCentralAnchor(List<Vector3> exclude)
    {
        List<Vector3> allAnchors = Methods.GetAllAnchors();
        Vector3 cenAnchor = Vector3.zero;
        float min = Mathf.Infinity;
        float temp;

        //if no anchor, return 0
        if (allAnchors.Count <= 0)
        {
            return Vector3.zero;
        }

        foreach (var anchor in allAnchors)
        {
            //exclude anchors in args.
            if (exclude.Contains(anchor))
            {
                continue;
            }

            temp = calcCentrality(anchor);
            if (temp < min)
            {
                min = temp;
                cenAnchor = anchor;
            }
        }

        return cenAnchor;
    }
}

