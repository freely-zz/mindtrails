/*
 * Tests out revised path-planner.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class A_PathPlanner : IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;
    private List<Vector3> trueAnchors = new List<Vector3>();
    private List<Vector3> plannedPath = new List<Vector3>();


    public List<Vector3> RegisterTrueAnchors()
    {
        //choose random anchor plus whichever other one is closest to it.
        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[0], trueAnchors));

        return trueAnchors;
    }


    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        //generate shortest path - may include reds (automatically constrained by diags setting)
        plannedPath = PathFinder.GetPath(trueAnchors[0], trueAnchors[1]);
        //strip out those extras
        plannedPath = PathFinder.StripPath(plannedPath);
        if (plannedPath.Count > 0)
            {
            actions1.Speed(5f);
            actions2.Speed(7f);

            //collect from generators with most reds
            int gridnum1 = Methods.MostRedGenerator();
            int gridnum2 = Methods.MostRedGenerator(gridnum1);

            actions1.CollectFromGen(gridnum1, carryLimit);
            actions2.CollectFromGen(gridnum2, carryLimit);

            //deposit non-reds randomly, reds on path
            int pathIndex = 0;
            int reds1 = Methods.RedsInGen(gridnum1);
            int reds2 = Methods.RedsInGen(gridnum2);
            List<int> colors1 = Methods.ColorsInGen(gridnum1);
            List<int> colors2 = Methods.ColorsInGen(gridnum1);

            if (reds1 + reds2 >= plannedPath.Count)//can win from pickups
            {
                for (int i = 0; i < reds1; i++)
                {
                    if (pathIndex < plannedPath.Count)
                        actions1.DepositAt(plannedPath[pathIndex++], Constants.RED);
                }
                for (int i = 0; i < reds2; i++)
                {
                    if (pathIndex < plannedPath.Count)
                        actions2.DepositAt(plannedPath[pathIndex++], Constants.RED);
                }
            }
            else
            {

                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum1, i) && pathIndex < (plannedPath.Count) && Methods.IsEmptyGrid(plannedPath[pathIndex]))
                    {
                        actions1.DepositAt(plannedPath[pathIndex]);
                        pathIndex++;
                    }
                    else
                    {
                        actions1.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
                    }
                }
                for (int i = 0; i < carryLimit; i++)
                {
                    if (Methods.IsRed(gridnum2, i) && pathIndex < (plannedPath.Count) && Methods.IsEmptyGrid(plannedPath[pathIndex]))
                    {
                        actions2.DepositAt(plannedPath[pathIndex]);
                        pathIndex++;
                    }
                    else
                    {
                        actions2.DepositAt(Methods.GetRandomEmptyGrid(1)[0]);
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



}
