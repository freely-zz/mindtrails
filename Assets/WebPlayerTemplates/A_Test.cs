/*
 * This script is used to test the APIs.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class A_Test // : IPlayerA
{
    private int gridSize = Config.gridSize;
    private int carryLimit = Config.carryLimit;
    private List<Vector3> trueAnchors = new List<Vector3>();


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

        actions1.Speed(5f);
        actions2.Speed(7f);

        //collect from generators with most reds
        int gridnum1 = Methods.MostRedGenerator();
        int gridnum2 = Methods.MostRedGenerator(gridnum1);

        actions1.CollectFromGen(gridnum1, carryLimit);
        actions2.CollectFromGen(gridnum2, carryLimit);

        List<Vector3> randoms = Methods.GetRandomEmptyGrid(carryLimit*2+1);
        for (int i = 0; i < carryLimit; i++)
        {
            actions1.DepositAt(randoms[i]);
        }
        for (int i = carryLimit; i < carryLimit*2; i++)
        {
            actions2.DepositAt(randoms[i]);
        }

        actions1.MoveTo(randoms[randoms.Count - 1]);
        actions2.MoveTo(randoms[randoms.Count - 1]);

        //i.e., park on top of each other.
        actions1.Park();
        actions2.Park();



        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
    }



}
