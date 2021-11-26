using System.Collections.Generic;
using UnityEngine;

public interface IPlayerA
{

    List<Vector3> RegisterTrueAnchors();
    List<Actions> MakeDecision(List<Vector3>blocks, int thisScore, float timeRemaining);
}
