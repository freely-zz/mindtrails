using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

public class A_simple : IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;
    private int shuttles = Config.shuttleNum;
    private List<Vector3> trueAnchors = new List<Vector3>();

    private int turn = 0;

    private List<Vector3> allBlocks = new List<Vector3>();
    private List<Vector3> allTokens = new List<Vector3>();
    private List<Vector3> allReds = new List<Vector3>();

    private Dictionary<List<Vector3>, List<Vector3>> paths = new Dictionary<List<Vector3>, List<Vector3>>();
    private Dictionary<List<Vector3>, List<Vector3>> zones = new Dictionary<List<Vector3>, List<Vector3>>();
    private Dictionary<List<Vector3>, float> suspicion = new Dictionary<List<Vector3>, float>();

    //list of keys used for dictionary
    private List<List<Vector3>> keyList = new List<List<Vector3>>();

    private List<Vector3> allPathsCells = new List<Vector3>();

    public List<Vector3> RegisterTrueAnchors()
    {
        //choose the 2 anchors thats most in the center.
        trueAnchors.Add(getCentralAnchor(new List<Vector3>()));
        trueAnchors.Add(getCentralAnchor(trueAnchors));

        keyList.Add(trueAnchors);
        iniKeys();

        //grab all zones for future usage.
        getAllZones();
        getAllPaths();
        iniSuspicion();

        return trueAnchors;
    }

    //TODO stop placing red tokens on anchors
    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        //allBlocks.Add(blocks.ToList<>);
        Methods.UnhighlightCells();
        turn++;
        allBlocks.Concat(blocks);
        Methods.HighlightCells(paths[keyList[0]]);

        List<Vector3> redsToPlace;

        getAllPaths();
        updateAllPathsCells();

        //TODO list of actions to accomidate diff numbers of shuttles.
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        int index = 0;

        //UnityEngine.Debug.Log((String)zones[0]);

        if (paths[keyList[0]].Count > 0)
        {
            //set speed to high
            actions1.Speed(20f);
            actions2.Speed(20f);

            //collect from generators with most reds
            int gridnum1 = Methods.MostRedGenerator();
            int gridnum2 = Methods.MostRedGenerator(gridnum1);

            actions1.CollectFromGen(gridnum1, carryLimit);
            actions2.CollectFromGen(gridnum2, carryLimit);

            //calculate how many red tokens we can deposit this turn
            int reds1 = Methods.RedsInGen(gridnum1);
            int reds2 = Methods.RedsInGen(gridnum2);
            int totReds = reds1 + reds2;

            //if we have enough reds to finish the path, do that.
            //TODO shorten this
            UnityEngine.Debug.Log(paths[keyList[0]].Count);
            if (totReds >= paths[keyList[0]].Count)
            {
                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum1, i) && index < paths[keyList[0]].Count)
                    {
                        actions1.DepositAt(paths[keyList[0]][index]);
                        allTokens.Add(paths[keyList[0]][index]);
                        index++;
                    }
                    else
                    {
                        Vector3 loc = nonRedTokenPlacement();
                        allTokens.Add(loc);
                        actions1.DepositAt(loc);
                    }
                }

                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum2, i) && index < paths[keyList[0]].Count)
                    {
                        actions2.DepositAt(paths[keyList[0]][index]);
                        allTokens.Add(paths[keyList[0]][index]);
                        index++;
                    }
                    else
                    {
                        Vector3 loc = nonRedTokenPlacement();
                        allTokens.Add(loc);
                        actions2.DepositAt(loc);
                    }
                }
            } else
            {
                //predict 1000 states
                redsToPlace = predictNext(totReds, 1000);

                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum1, i))
                    {
                        actions1.DepositAt(redsToPlace[index]);
                        allTokens.Add(redsToPlace[index]);
                        index++;
                    }
                    else
                    {
                        Vector3 loc = nonRedTokenPlacement();
                        allTokens.Add(loc);
                        actions1.DepositAt(loc);
                    }
                }

                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum2, i))
                    {
                        actions2.DepositAt(redsToPlace[index]);
                        allTokens.Add(redsToPlace[index]);
                        index++;
                    }
                    else
                    {
                        Vector3 loc = nonRedTokenPlacement();
                        allTokens.Add(loc);
                        actions2.DepositAt(loc);
                    }
                }

            }

            actions1.Park();
            actions2.Park();

        }
        /*
        foreach (var b in paths)
        {
            Methods.HighlightCells(b.Value);
        }
        */
        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
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

    //get all zones between all anchors.
    public void getAllZones()
    {
        foreach (var key in keyList)
        {
            zones.Add(key, selectZone(key[0], key[1]));
        }
    }

    //get all paths between all anchors.
    public void getAllPaths()
    {
        paths.Clear();
        foreach (var key in keyList)
        {
            paths.Add(key, pathGen(key[0], key[1]));
        }
    }

    //Initialize suspicion model
    public void iniSuspicion()
    {
        List<Vector3> anchors = Methods.GetAllAnchors();

        foreach (var key in keyList)
        {
            suspicion.Add(key, 1 / zones.Count);
        }
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

    //predit "predictCount" amount of future turns, redsNum is number of reds available this turn
    public List<Vector3> predictNext(int redsNum, int predictCount)
    {
        int i, j;
        List<Vector3> distribution;
        Vector3 temp;
        float Score = Mathf.NegativeInfinity;
        float tempScore;
        List<Vector3> predictDist = new List<Vector3>();

        //for each generation of predicted next state
        for (i = 0; i < predictCount; i++)
        {
            distribution = new List<Vector3>();

            //for each red available this turn, add it to somewhere random along any paths.
            for (j = 0; j < redsNum; j++)
            {
                temp = allPathsCells[Random.Range(0, allPathsCells.Count)];
                //avoid duplications.
                while(distribution.Contains(temp))
                {
                    temp = allPathsCells[Random.Range(0, allPathsCells.Count)];
                }
                distribution.Add(temp);
            }

            //"predicted" block locations
            List<Vector3> predictedBlocks = blockPredict(distribution);
            //"predicted" suspicion
            Dictionary<List<Vector3>, float> susP = updatedSus(predictedBlocks);
            //predict how good next state is, if better than current, update it.
            tempScore = BoardScore(susP[trueAnchors], distribution);
            if (tempScore > Score)
            {
                Score = tempScore;
                predictDist = new List<Vector3>(distribution);
            }
        }

        return predictDist;
    }

    //"predict" where blocks will be placed given where red tokens will be placed.
    //TODO incorporate suspicion for prediction.
    public List<Vector3> blockPredict(List<Vector3> distribution)
    {
        List<Vector3> predictedBlocks = new List<Vector3>();
        int i = 0;
        //assume the blocks will be placed next to red tokens.

        //randomly choose one of the red block we're placing down, place the block right next to it
        for(i = 0; i < 3 ; i++)
        {
            predictedBlocks.Add(randNearBlock(distribution[Random.Range(0, distribution.Count)], 1));
        }

        return predictedBlocks;
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

    //calculate the weight of a cell, in more intersects = higher weight.
    public int cellWeight(Vector3 cell)
    {
        int weight = 0;

        //for each zone the cell belongs to, get weight.
        foreach (var zone in zones)
        {
            if (zone.Value.Contains(cell))
            {
                weight++;
            }
        }
        return weight;
    }

    //combine all paths into a single array.
    public void updateAllPathsCells()
    {
        allPathsCells = new List<Vector3>();
        foreach(var path in paths)
        {
            allPathsCells = allPathsCells.Concat(path.Value).Distinct().ToList();
        }
    }

    //get a "predicted" suspicion model based on "predicted" blocks
    public Dictionary<List<Vector3>, float> updatedSus(List<Vector3> blocks)
    {
        Dictionary<List<Vector3>, float> susCopy = new Dictionary<List<Vector3>, float>(suspicion);
        Dictionary<List<Vector3>, float> weights = new Dictionary<List<Vector3>, float>();
        //get weights

        int weight;
        int totalWeight = 0;

        //initialize weights as 0 for each zone.

        foreach(var key in keyList)
        {
            weights.Add(key, 0.0f);
        }

        //for each block, calculate weight, and update weight of each zone.
        foreach (var block in blocks)
        {
            
            weight = cellWeight(block);
            totalWeight += weight;

            foreach (var zone in zones)
            {
                if (zone.Value.Contains(block))
                {
                    weights[zone.Key] += weight;
                }
            }
        }

        //for each key/zone

        foreach(var key in keyList)
        {
            foreach(var w in weights)
            {
                if (key != w.Key)
                {
                    susCopy[key] += susCalc(suspicion[w.Key], weights[key] / totalWeight);
                    susCopy[key] -= susCalc(suspicion[key], w.Value / totalWeight);
                }
            }
        }

        return susCopy;
    }

    //yellow/blue token placement
    //TODO better logic than random.
    public Vector3 nonRedTokenPlacement()
    {
        return Methods.GetRandomEmptyGrid(1)[0];
    }

    //initialize keys for dictionaries, each key is a pair of anchor v3.
    public void iniKeys()
    {
        List<Vector3> allAnchors = Methods.GetAllAnchors();
        List<Vector3> pairs = new List<Vector3>();
        int i, j;

        for (i=0; i< allAnchors.Count-1; i++)
        {
            for(j=i+1;j<allAnchors.Count; j++)
            {
                //if the current pair is not true anchor.
                if(!(keyList[0].Contains(allAnchors[i]) && keyList[0].Contains(allAnchors[j])))
                {
                    pairs = new List<Vector3>();
                    pairs.Add(allAnchors[i]);
                    pairs.Add(allAnchors[j]);
                    keyList.Add(pairs);
                }
            }
        }
    }

    /***************************************************Math Functions**************************************************/

        //path generation algorithm
    public List<Vector3> pathGen(Vector3 a1, Vector3 a2)
    {
        return PathFinder.StripPath(PathFinder.GetPath(a1, a2));
    }

    //update suspicion function here.
    public float susCalc(float conf, float susWeight)
    {
        //some constant to imitate something of a learning rate.
        float learningRate = 1 / 3;
        //magic number to control the impact of what turn it is has on the score.
        int constant = 100;
        return susWeight * (learningRate * (conf + turn / constant));
    }

    //assign some score for the "predicted" board state.
    public float BoardScore(float suspicion, List<Vector3> distribution)
    {
        //calculate increase in path completion.
        int pathLength = paths[trueAnchors].Count();
        int newRed = distribution.Intersect(paths[trueAnchors]).Count();

        //how much do we value path completion.
        float constant = 2;

        //simple formula that uses difference in suspicion and increase in path completion
        return 1 - 2 * suspicion + constant * (newRed/pathLength);
    }

}

