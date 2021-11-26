/*
 * This script demos stacking and unstacking counters.
 */

using System.Collections.Generic;
using UnityEngine;

public class A_Demo: IPlayerA
{
    int gridSize = Config.gridSize;
    int carryLimit = Config.carryLimit;

    public List<Vector3> RegisterTrueAnchors()
    {
        List<Vector3> trueAnchors = new List<Vector3> { Vector3.zero, Vector3.zero };
        return trueAnchors;
    }


    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();
        Methods.UnhighlightCells();
        List<Vector3> posList = Methods.GetRandomEmptyGrid(10);
        Methods.HighlightCells(posList);
        //keep things moving
        actions1.Speed(6f);
        //randomly select generator
        actions1.CollectFromGen(Random.Range(0, 3), carryLimit);

        //List<Vector3> posList = Methods.GetRandomEmptyGrid(15);
        actions1.DepositAt(posList[0]);
        actions1.DepositAt(posList[1]);
        actions1.DepositAt(posList[0]);//stack

        actions1.CollectFromGrid(posList[1]);

        actions1.DepositAt(posList[0]);//stack      
        
        actions1.CollectFromGrid(posList[0]);
        actions1.CollectFromGrid(posList[0]);
        actions1.CollectFromGrid(posList[0]);
        actions1.CollectFromGrid(posList[0]);
        
        for (int i = 5; i < 9; i++)
            actions1.DepositAt(posList[i]);

        actions1.Speed(7f);
        for (int i = 8; i > 4; i--)
            actions1.MoveTo(posList[i]);


        actions1.Park();

        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
    }



}
