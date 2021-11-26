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

public class rj_model_4: IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;
    private int shuttles = Config.shuttleNum;
    private List<Vector3> trueAnchors = new List<Vector3>();
    // private Vector3 mid = new Vector3();

    private int turn = 0;
    private int total_token_place = 0;

    public float Confidence = 0.4f;
    public float learning_rate = 0.6f;

    private Vector3 mid_point = new Vector3();

    // variables used for convincers
    private Vector3 park_location1 = new Vector3();
    private Vector3 park_location2 = new Vector3();
 


    // private List<Vector3> allBlocks = new List<Vector3>();
    private List<Vector3> allTokens = new List<Vector3>();

    private List<Vector3> redtokens = new List<Vector3>();
    private List<Vector3> nonREDtokens = new List<Vector3>();
    // private List<Vector3> allReds = new List<Vector3>();

    private Dictionary<List<Vector3>, List<Vector3>> paths = new Dictionary<List<Vector3>, List<Vector3>>();
    private Dictionary<List<Vector3>, List<Vector3>> zones = new Dictionary<List<Vector3>, List<Vector3>>();

    private Dictionary<List<Vector3>, float> Belief = new Dictionary<List<Vector3>, float>();
    private Dictionary<List<Vector3>, float> Trust = new Dictionary<List<Vector3>, float>();
         // keyList_MIDpoint - has a pair of anchers and their mid-point 
    private Dictionary<List<Vector3>, Vector3> KeyList_MIDpoint = new Dictionary<List<Vector3>, Vector3>();


    //list of keys used for dictionary
    private List<List<Vector3>> keyList = new List<List<Vector3>>();

    private List<Vector3> allPathsCells = new List<Vector3>();

    public List<Vector3> RegisterTrueAnchors()
    {
        //choose the 2 anchors.

        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[0], trueAnchors));

        keyList.Add(trueAnchors);
        iniKeys(); // pair all the anchers all possible combination a3,a5 - a1,a2 a1,a3 ......

        //grab all zones for future usage.
        getAllZones();

        //will get all the path between the anchor pairs
        getAllPaths(); 

        // will initialize --- initial belief b1 (what agent thinks about the belief of human’s belief ) 
        init_belief();

        // show_belief();
        // gets all the mid-points of all pair of anchers
        iniMidpoints();


        // Methods.HighlightCells(paths[keyList[0]]);
        // for (int i=1; i<=5 ;i++)
        // {
        //    Methods.HighlightCells(paths[keyList[i]]); 
        // }
        return trueAnchors;
    }

    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
       
        // Methods.UnhighlightCells();
        // total_token_place - is the total number of tokens that will be placed after this turn. 
        // this value is used in the reward function 
        if (turn == 0)
        {
            if(shuttles == 2)
            {
                total_token_place = 8;
            }
            else
            {
                total_token_place = 4;
            }
        }
        else if (turn > 0)
        {
            // confidence C is updated every turn 
            update_confidence();
            // Belief is updated according to the blocks placed by the human player.
            Belief = update_belief(blocks);
            // show_belief();

            // show_Trust();

            if(shuttles == 2)
            {
                total_token_place = allTokens.Count + 8;
            }
            else
            {
                total_token_place = allTokens.Count + 4;
            }
        }



        // allBlocks.Concat(blocks);

        List<Vector3> tokensToPlace;
        List<Vector3> Red_to_place = new List<Vector3>();
        List<Vector3> NonRed_to_place = new List<Vector3>();       

        getAllPaths();
        updateAllPathsCells();

        // Methods.HighlightCells(paths[keyList[0]]);

        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        int index = 0;
        int R_index = 0;
        int NR_index = 0;

        // UnityEngine.Debug.Log((String)zones[0]);

        if (paths[keyList[0]].Count > 0)
        {
            //set speed to high
            actions1.Speed(25f);
            actions2.Speed(20f);

            //collect from generators with most reds
            int gridnum1 = Methods.MostRedGenerator();
            int gridnum2 = Methods.MostRedGenerator(gridnum1);

            actions1.CollectFromGen(gridnum1, carryLimit);
            actions2.CollectFromGen(gridnum2, carryLimit);

            //calculate how many red tokens we can deposit this turn
            int reds1 = Methods.RedsInGen(gridnum1);
            int reds2 = Methods.RedsInGen(gridnum2);
            int totReds = 0;
            int tot_NONreds = 0;

            if (shuttles == 2)
            {
                totReds = reds1 + reds2;
                tot_NONreds = 8 - totReds;

            } 
            else if (shuttles == 1)
            {
                totReds = reds1;
                tot_NONreds = 4 - tot_NONreds;
            }


            //if we have enough reds to finish the path, do that.
            UnityEngine.Debug.Log("@@@@@@@- length of the true path");
            UnityEngine.Debug.Log(paths[keyList[0]].Count);
            if (totReds >= paths[keyList[0]].Count)
            {

                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum1, i) && index < paths[keyList[0]].Count)
                    {
                        actions1.DepositAt(paths[keyList[0]][index]);
                        allTokens.Add(paths[keyList[0]][index]);
                        redtokens.Add(paths[keyList[0]][index]);
                        index++;
                    }
                    else
                    {
                        Vector3 loc = nonRedTokenPlacement();
                        allTokens.Add(loc);
                        nonREDtokens.Add(loc);
                        actions1.DepositAt(loc);
                    }
                }
                if (shuttles == 2)
                {
                    for (int i = 0; i < carryLimit; i++)
                    {
                        if (Methods.IsRed(gridnum2, i) && index < paths[keyList[0]].Count)
                        {
                            actions2.DepositAt(paths[keyList[0]][index]);
                            allTokens.Add(paths[keyList[0]][index]);
                            redtokens.Add(paths[keyList[0]][index]);
                            index++;
                        }
                        else
                        {
                            Vector3 loc = nonRedTokenPlacement();
                            allTokens.Add(loc);
                            nonREDtokens.Add(loc);
                            actions2.DepositAt(loc);
                        }
                    }
                }
                
            }
            else
            {
                //predict number of states
                tokensToPlace = predictNext(totReds, tot_NONreds, 100);
                // splitting red locations and non-red locations 
                for (int x = 0; x < totReds; x++)
                {
                    Red_to_place.Add(tokensToPlace[x]);
                }

                for (int y = totReds; y < (totReds+tot_NONreds); y++)
                {
                    NonRed_to_place.Add(tokensToPlace[y]);
                }

                // placing counters according to above locations
                for (int i = 0; i < carryLimit; i++)
                {

                        if (Methods.IsRed(gridnum1, i))
                        {
                            actions1.DepositAt(Red_to_place[R_index]);
                            allTokens.Add(Red_to_place[R_index]);
                            R_index++;
                        }
                        else
                        {
                            actions1.DepositAt(NonRed_to_place[NR_index]);
                            allTokens.Add(NonRed_to_place[NR_index]);
                            NR_index++;
                        }                                        
 
                }
                if (shuttles == 2)
                {
                    for (int i = 0; i < carryLimit; i++)
                    {
                       
                        if (Methods.IsRed(gridnum2, i))
                        {
                            actions2.DepositAt(Red_to_place[R_index]);
                            allTokens.Add(Red_to_place[R_index]);
                            R_index++;
                        }
                        else
                        {
                            actions2.DepositAt(NonRed_to_place[NR_index]);
                            allTokens.Add(NonRed_to_place[NR_index]);
                            NR_index++;
                        }   
                    }
                }  

            }

            // using 
        
            if (redtokens.Count >= 1 && turn >= 1)
            {
                find_park();

                actions1.Speed(15f);
                actions2.Speed(15f);   

                actions1.MoveTo(park_location1);
                actions2.MoveTo(park_location2);
            }

            turn++;

            actions1.Park();
            actions2.Park();

        }


        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
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
    public List<Vector3> predictNext(int redsNum, int NON_redsNum, int predictCount)
    {
        int i, j, k;



        List<Vector3> distribution;
        List<Vector3> NON_RED_distribution;

        Vector3 temp;
        float Score = Mathf.NegativeInfinity;
        float tempScore;
        List<Vector3> predict_RED_Dist = new List<Vector3>();
        List<Vector3> predict_nonRED_Dist = new List<Vector3>();
        List<Vector3> predict_Dist = new List<Vector3>();


        //for each generation of predicted next state
        for (i = 0; i < predictCount; i++)
        {
            distribution = new List<Vector3>(); // locations where red tokens can be placed
            NON_RED_distribution = new List<Vector3>(); // locations where blue,yellow tokens can be placed 
            float MIN_susp = 0.0f;

            //for each red available this turn, add it to somewhere random along any paths.
            for (j = 0; j < redsNum; j++)
            {
                temp = allPathsCells[Random.Range(0, allPathsCells.Count)];
                //avoid duplications.
                while (distribution.Contains(temp))
                {
                    temp = allPathsCells[Random.Range(0, allPathsCells.Count)];
                }
                distribution.Add(temp);
            }
            // for each yellow,blue available this turn, add it somewhere near paths (allpathcells).
            for (k = 0; k < NON_redsNum; k++)
            {
                temp = nonRedTokenPlacement();
                //avoid duplications.
                while (NON_RED_distribution.Contains(temp))
                {
                    temp = nonRedTokenPlacement();
                }
                NON_RED_distribution.Add(temp);
            }

            //"predicted" block locations
            List<Vector3> predictedBlocks = blockPredict(distribution);
            //"predicted" belief
            Dictionary<List<Vector3>, float> belief_P = update_belief(predictedBlocks);
            // "predicted" suspition of each zone
            Dictionary<List<Vector3>, float> suspition_P = update_Suspition(distribution, NON_RED_distribution);


            // finding the minimum suspition value - used in reward function (Reward_fun).
            MIN_susp = Find_min_suspition(suspition_P);

            //predict how good next state is, if better than current, update it.
            tempScore = Reward_fun(belief_P, suspition_P, MIN_susp, distribution);
            if (tempScore > Score)
            {
                Score = tempScore;
                predict_RED_Dist = new List<Vector3>(distribution);
                predict_nonRED_Dist = new List<Vector3>(NON_RED_distribution);
                // adding trust to get the results - in experiment
                Trust = new Dictionary<List<Vector3>, float>(suspition_P);

            }
        }
        // if (turn == 0)
        // {
        //     show_Trust();
        // }

        redtokens.AddRange(predict_RED_Dist);
        nonREDtokens.AddRange(predict_nonRED_Dist);
        // concat predicted red and non-red locations
        // then shuffle them 
        predict_Dist = concat_predictions(predict_RED_Dist, predict_nonRED_Dist);

        return predict_Dist;
    }

    //"predict" where blocks will be placed given where red tokens will be placed.
    public List<Vector3> blockPredict(List<Vector3> distribution)
    {
        List<Vector3> predictedBlocks = new List<Vector3>();
        int i = 0;
        //assume the blocks will be placed next to red tokens.

        //randomly choose one of the red block we're placing down, place the block right next to it
        for (i = 0; i < 3; i++)
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



    //combine all paths into a single array.
    public void updateAllPathsCells()
    {
        allPathsCells = new List<Vector3>();
        foreach (var path in paths)
        {
            allPathsCells = allPathsCells.Concat(path.Value).Distinct().ToList();
        }
    }

    //update_Suspition is just a function to find the ratio of red and non-red counters inside a zone.
    // this is used to find a negative reward value , which is used in the reward function.
    public Dictionary<List<Vector3>, float> update_Suspition(List<Vector3> distribution, List<Vector3> NON_RED_distribution)
    {
        Dictionary<List<Vector3>, float> suspition = new Dictionary<List<Vector3>, float>();
        float s;
        // float t_sum = 0.0f;
        int avg_size = 0;
        float nor_size;

        foreach (var key in keyList)
        {
            suspition.Add(key, 0.0f);
        }

        foreach (var zone in zones)
        {
            avg_size += zone.Value.Count;
        }
        // used for normalizing the susption value
        nor_size = ((float)avg_size / (float)zones.Count);

        foreach (var zone in zones)
        {
            int NR = 0;
            int NOR = 0;

            foreach(var loc in distribution)
            {
                if(zone.Value.Contains(loc))
                {
                    NR++;
                }
            }

            foreach(var loc1 in NON_RED_distribution)
            {
                if(zone.Value.Contains(loc1))
                {
                    NOR++;
                }
            }
            if (turn > 0)
            {
                foreach(var loc in redtokens)
                {
                    if(zone.Value.Contains(loc))
                    {
                        NR++;
                    }
                }

                foreach(var loc1 in nonREDtokens)
                {
                    if(zone.Value.Contains(loc1))
                    {   
                        NOR++;
                    }
                }                
            }
            s = ( ((float)NR + ((float)NOR/(float)total_token_place))/ nor_size );
            suspition[zone.Key] = s;
        } 



        return suspition;
    }

    //yellow/blue token placement
    public Vector3 nonRedTokenPlacement()
    {
        Vector3 r;
        r = allPathsCells[Random.Range(0, allPathsCells.Count)];
        return randNearBlock(r, gridSize/10);
    }

    //initialize keys for dictionaries, each key is a pair of anchor v3.
    public void iniKeys()
    {
        List<Vector3> allAnchors = Methods.GetAllAnchors();
        List<Vector3> pairs = new List<Vector3>();
        int i, j;

        for (i = 0; i < allAnchors.Count - 1; i++)
        {
            for (j = i + 1; j < allAnchors.Count; j++)
            {
                //if the current pair is not true anchor.
                if (!(keyList[0].Contains(allAnchors[i]) && keyList[0].Contains(allAnchors[j])))
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
        List<Vector3> path = new List<Vector3>();

        path.AddRange(PathFinder.StripPath(PathFinder.GetPath(a1, a2)));

        return path;

    }

    //assign some score for the "predicted" board state.
    public float Reward_fun(Dictionary<List<Vector3>, float> belief_P, Dictionary<List<Vector3>, float> suspition_P, float MIN_susp, List<Vector3> distribution)
    {
        //calculate increase in path completion.
        int pathLength = paths[trueAnchors].Count();
        int newRed = distribution.Intersect(paths[trueAnchors]).Count();

        //simple formula that uses difference in suspicion and increase in path completion
        
        return ((1 - (belief_P[trueAnchors] - Belief[trueAnchors])) + ((float)newRed / (float)pathLength) + (MIN_susp - suspition_P[trueAnchors]) );
    }

    // find all the mid-points of all pairs of anchers in keypair dictonary
    public void iniMidpoints()
    {
        foreach (var key in keyList)
        {
            float tx = 0.0f;
            float ty = 0.0f;
            float tx1 = 0.0f;
            float ty1 = 0.0f;

            tx = key[0][0];
            ty = key[0][1];

            tx1 = key[1][0];
            ty1 = key[1][1];

            mid_point = new Vector3((float)(tx+tx1) / 2.0f, (float)(ty+ty1) / 2.0f, 0.0f); 
            KeyList_MIDpoint.Add(key, mid_point);
        }
    }

    // updates the confidence value - used in updating the belief value
    public void update_confidence()
    {
        Confidence = (((1.0f - Confidence)*learning_rate) + Confidence );

        UnityEngine.Debug.Log("@@@@@@@@@@@@@@--updated-Confidence");
        UnityEngine.Debug.Log(Confidence);
    }

    // at the begining of the game belief is set to 1 divided by tottal number of zones
    public void init_belief()
    {
        foreach (var key in keyList)
        {
            Belief.Add(key, 1f / zones.Count);
        }
    }
    // belief is updated 
    public Dictionary<List<Vector3>, float> update_belief(List<Vector3> blocks)
    {
        Dictionary<List<Vector3>, float> BeliefCopy = new Dictionary<List<Vector3>, float>(Belief);
        float b;
        float b_sum = 0.0f;        
        float min_distance;
        foreach(var key in keyList)
        {
            b = 0.0f;
           foreach(var block in blocks)
           {
            // to find which zone (2 ancher points closer to the current block)
            // min_distance = Vector3.Distance(block,KeyList_MIDpoint[key]);
                min_distance = find_min_distance(block);
            // after finding which zone(2 anchers closer to the current block)
            // use that to update the belief of each zone

                if (min_distance == Vector3.Distance(block,KeyList_MIDpoint[key]))
                {
                    b += (((1.0f - Confidence) * BeliefCopy[key] ) + Confidence ) ;
                } 
                else
                {
                    b += ((1.0f - Confidence) * BeliefCopy[key] ) ;
                }

            // updating Belief value
            } 
           BeliefCopy[key] = ((float)b /(float)3.0);
        }
        foreach(var key in keyList)
        {
            b_sum += BeliefCopy[key];
        }
        foreach(var key in keyList)
        {
            BeliefCopy[key] = ( BeliefCopy[key] / b_sum );
        }         
       return BeliefCopy; 
    }

    // returns the minimum suspition value - state of envirionment - which has the lowest value
    public float Find_min_suspition (Dictionary<List<Vector3>, float> suspition)
    {
        float min = Mathf.Infinity;
        foreach (var susp in suspition)
        {
            if(min >= susp.Value)
            {
                min = susp.Value;
            }
        }
        return min;
    }

    // finds which zone is closer to the given block
    public float find_min_distance (Vector3 block)
    {
        float shortest_distance = Mathf.Infinity;

        foreach(var key in keyList)
        {
            if (shortest_distance >= Vector3.Distance(block,KeyList_MIDpoint[key]))
            {
                shortest_distance = Vector3.Distance(block,KeyList_MIDpoint[key]);
            }
        }

        return shortest_distance;
    }

    // concats and shuffles the list
    public List<Vector3> concat_predictions (List<Vector3> predict_RED_Dist, List<Vector3> predict_nonRED_Dist)
    {

        predict_RED_Dist.AddRange(predict_nonRED_Dist);
        // List<Vector3> s_list = Shuffle(predict_RED_Dist);
        List<Vector3> s_list = new List<Vector3>(predict_RED_Dist);

        return s_list;
    }

    // public void show_belief()
    // { string s_data;
    //     StreamWriter writer = new StreamWriter("/Users/ramanajeyaprakash/Desktop/Project/algo-mindt/exp/belief_values.txt", true);
    //     foreach(var b in Belief)
    //     {

    //         UnityEngine.Debug.Log("@@@@@@@- belief value");
    //         UnityEngine.Debug.Log(b.Value);
    //         s_data = b.Value.ToString() + "\t";
    //         writer.Write(s_data);
    //     }
    //     writer.Close();
    // }

    // public void show_Trust()
    // { string t_data;
    //     StreamWriter writer1 = new StreamWriter("/Users/ramanajeyaprakash/Desktop/Project/algo-mindt/exp/Trust_value.txt", true);

    //     foreach(var T in Trust)
    //     {
    //         UnityEngine.Debug.Log("@@@@@@@- Trust value");
    //         UnityEngine.Debug.Log(T.Value);  
    //         t_data = T.Value.ToString() + "\t";
    //         writer1.Write(t_data);                     
    //     }
    //     writer1.Close();
    // }    

    // after placing tokens moves hand to 2 locations to deceive human
    public void find_park()
    {
        int i;
        int index_of_largest = 0 ;
        int index_of_second_largest = 0;
        float max_belief = Mathf.NegativeInfinity;
        float max_belief2 = Mathf.NegativeInfinity;
 

        for (i = 1 ; i < keyList.Count ; i++)
        {
            if(max_belief < Belief[keyList[i]])
            {
                max_belief2 = max_belief;
                max_belief = Belief[keyList[i]];
                index_of_largest = i;
            }
            else if (Belief[keyList[i]] > max_belief2)
            {
                max_belief2 = Belief[keyList[i]];
                index_of_second_largest = i;
            }
        }


        park_location1 = new Vector3(0f, 0f,0f);
        while( (!zones[keyList[index_of_largest]].Contains(park_location1)) && (zones[trueAnchors].Contains(park_location1)) ) 
        {
            park_location1 = redtokens[Random.Range(0,redtokens.Count)];

        }
        park_location2 = new Vector3(0f, 0f,0f);
        while((!zones[keyList[index_of_second_largest]].Contains(park_location2)) && (zones[trueAnchors].Contains(park_location2)) )
        {
            park_location2 = redtokens[Random.Range(0,redtokens.Count)];
           
        }


    }




}

   


