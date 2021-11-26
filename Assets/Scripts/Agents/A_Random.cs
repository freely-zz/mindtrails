using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class A_Random : IPlayerA
{
    int MAX_CARRY = Config.carryLimit;
    int gridsize = Config.gridSize;
    bool diags = Config.diags;

    List<Vector3> parkingPositions = new List<Vector3>();
    List<Vector3> anchorPositions = new List<Vector3>();


    public List<Vector3> RegisterTrueAnchors()
    {
        List<Vector3> trueAnchors = new List<Vector3>();

        trueAnchors.Add(Methods.RandomAnchor(trueAnchors));
        trueAnchors.Add(Methods.SearchClosestAnchor(trueAnchors[0], trueAnchors));

        return trueAnchors;
    }


    public List<Actions> MakeDecision(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Actions actions1 = new Actions();
        Actions actions2 = new Actions();

        //randomly select generators 0 - 3
        int genChoice1 = Random.Range(0, 3);
        int genChoice2 = Random.Range(0, 3);
        while (genChoice2 == genChoice1)
        {
            genChoice2 = Random.Range(0, 3);
        }

        //give shuttles random speeds
        actions1.Speed(Random.Range(2.5f, 7f));
        actions2.Speed(Random.Range(2.5f, 7f));

        //collect as many counters as allowed
        actions1.CollectFromGen(genChoice1, MAX_CARRY);
        actions2.CollectFromGen(genChoice2, MAX_CARRY);

        //deposit them in any empty grid locations
        List<Vector3> posList = Methods.GetRandomEmptyGrid(8);
        for (int i = 0; i < 4; i++)
            actions1.DepositAt(posList[i]);
            actions1.Speed(Random.Range(2.5f, 7f));


        for (int i = 4; i < 8; i++)
            actions2.DepositAt(posList[i]);
            actions2.Speed(Random.Range(2.5f, 7f));

        //park off-grid, wherever's nearest to where you end up.
        actions1.Park();
        actions2.Park();


        //always return list of Actions objects
        List<Actions> AIactions = new List<Actions>();
        AIactions.Add(actions1);
        AIactions.Add(actions2);
        return AIactions;
    }

}
