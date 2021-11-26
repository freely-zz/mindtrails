using System.Collections.Generic;
using UnityEngine;

public interface IPlayerB
{

    List<Vector3> DecideBlocks(List<Actions> AIactions, List<Vector3> convincers, float timeRemaining);


}
