/*
 * Tests out revised path-planner.

 find score form Calcscore function and 
 if score is possitive 
 true anchors are tried to connected 
 else fake anchors are connected.
*/ 

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Net;
using Random = UnityEngine.Random;

public class A_RJ2 : IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;////////
    private int anchor_size;
    private int total_fakepath; // has the number of fake path (ie) path b/w pair of fake anchers 
    private List<Vector3> trueAnchors = new List<Vector3>();
    private List<Vector3> plannedPath = new List<Vector3>();
    private List<List<Vector3>> fakePath = new List<List<Vector3>>();
    private List<Vector3> total_anchor = new List<Vector3>();
    private float myscore = 0;

    private List<Vector3> allFakeAnchors = new List<Vector3>();

    public List<Vector3> RegisterTrueAnchors()
    {
        //choose random anchor plus whichever other one is closest to it.
        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[0], trueAnchors));

        total_anchor = Methods.GetAllAnchors();
        anchor_size = total_anchor.Count;
        //trying to add all the anchors to true anchors

        int total_fake_anchors = anchor_size-2;

        for (int i = 2; i < total_fake_anchors-2; i=i+2)
        {
        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[i], trueAnchors));
        }
        return trueAnchors;
    }


    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        //float randomness = 0.7f;
/*
planned path contains the path between the 2 true anchors that the program is trying to connect.

*/
        plannedPath = PathFinder.GetPath(trueAnchors[0], trueAnchors[1]);
        //strip out those extras
        plannedPath = PathFinder.StripPath(plannedPath);
        /*
                // finding all the fake anchors path 

                the below for loop tries to get all the path betweeen the anchors and add it to 
                the fakepath[][] list ie.  fakepath[1] = path between trueanchor[2] and trueanchor[3] and so on.

        */

        int total_fake_anchors = anchor_size - 2;

        for (int i = 2, j=0; i < total_fake_anchors-2; i=i+2, j++) 
        {
        
            fakePath.Add(PathFinder.GetPath(trueAnchors[i], trueAnchors[i+1]));
            fakePath[j] = PathFinder.StripPath(fakePath[j]);
            total_fakepath = j;
        }

        if (plannedPath.Count > 0)
        {
            actions1.Speed(7f);
            actions2.Speed(11f);

            //collects from a random genrator and 
            //a generators with most reds
            int gridnum1 = Random.Range(0, 3);
            int gridnum2 = Methods.MostRedGenerator(gridnum1);

            actions1.CollectFromGen(gridnum1, carryLimit);
            actions2.CollectFromGen(gridnum2, carryLimit);

            //deposit non-reds randomly, reds on path
            int pathIndex = 0;



            myscore = CalcScore(blocks,myscore);


            
/*
if the claculated score is '+ve' then 
    the if condition is true. and we will fill the path between the true anchors (ie) we fill the plannedPath.
else
    
    to deceive the human player we try to fill the fakepath[i] - we pick a random fakepath[random] and fill the path,
    this is repeated till the score becomes '+ve' 





*/
            if (myscore>=0)
            {
                for (int i = 0; i < carryLimit; i++)
                {   
                    int randomSpot = Random.Range(0, plannedPath.Count);

                    if (Methods.IsRed(gridnum1, i) && pathIndex < (plannedPath.Count) && Methods.IsEmptyGrid(plannedPath[randomSpot]))
                    {
                        actions2.DepositAt(plannedPath[randomSpot]);
                    }
                    else
                    {
                        actions2.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                }
                for (int i = 0; i < carryLimit; i++)
                {
                    //float e = Random.Range(0.0f, 1.0f);
                    int randomSpot = Random.Range(0, plannedPath.Count);

                    actions1.DepositAt(plannedPath[randomSpot]);

                    if (Methods.IsRed(gridnum2, i) &&  Methods.IsEmptyGrid(plannedPath[randomSpot]))
                    {
                        plannedPath = PathFinder.StripPath(plannedPath);
                    }
                    else
                    {
                        actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]); // try to place the non-red colours near path of fill them with fake path
                    }
                }
               
            }
            /*
            select a random fakepath and fill , till the score is possitive

            */
            else
            {   //selecting a random fake path from the list.
                int random_fakepath = Random.Range(0, total_fakepath);

                for (int i = 0; i < carryLimit; i++)
                {   // selecting a random block from a path
                    int randomSpot2 = Random.Range(0, fakePath[random_fakepath].Count);
                    

                    if (Methods.IsRed(gridnum1, i) && pathIndex < (fakePath[random_fakepath].Count) && Methods.IsEmptyGrid(fakePath[random_fakepath][randomSpot2]))
                    {
                            actions2.DepositAt(fakePath[random_fakepath][randomSpot2]);
                    }
                    else
                    {
                        actions2.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                }
                for (int i = 0; i < carryLimit; i++)                  //12 
                {
                    //float e = Random.Range(0.0f, 1.0f);
                    int randomSpot2 = Random.Range(0, fakePath[random_fakepath].Count);

                    actions1.DepositAt(fakePath[random_fakepath][randomSpot2]);

                    if (Methods.IsRed(gridnum2, i) && Methods.IsEmptyGrid(fakePath[random_fakepath][randomSpot2]))
                    {
                        fakePath[random_fakepath] = PathFinder.StripPath(fakePath[random_fakepath]);
                    }
                    else
                    {
                        actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                }

            }

            actions1.Park();
            actions2.Park();
        }

        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
    }

    //find the eqli distance between blocks and anchor and fins the score.
    // the CalcScore(List<Vector3> blocks, float score) returns possive value if the user places the block far from the 
    // plannedPath (true-anchors) and negative value if blocks are placed near the PlannedPath. 

    public float CalcScore(List<Vector3> blocks, float score)
    {
        float dx, dy;
        float distance1, distance2, distance3;

        //UnityEngine.Debug.Log("#################");
        //UnityEngine.Debug.Log(score);
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
            
        }

        return score;
    }

}


