using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B_Random : IPlayerB
{
    public List<Vector3> DecideBlocks(List<Actions> AIactions, List<Vector3> convincers, float timeRemaining)
    {
        return Methods.GetRandomEmptyGrid(Config.blocksPerTurn);
    }
}
