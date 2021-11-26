using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathFinder
{
    /*pathfinding methods*/


    class SearchNode
    {
        internal Vector3 position;
        internal SearchNode parent;
        internal float g;   //steps travelled so far
        internal float f;   //function - f=g+h where h = heuristic

        internal SearchNode(Vector3 pos, SearchNode par, float g, float f)
        {
            this.position = pos;
            this.parent = par;
            this.g = g;
            this.f = f;
        }

        public static int CompareNodes(SearchNode a, SearchNode b)
        {
            if (a.f > b.f) return 1;
            else if (a.f < b.f) return -1;
            else return 0;
        }
    }


    static public List<Vector3> StripPath(List<Vector3> originalPath)
    {
        List<Vector3> returnList = new List<Vector3>();
        foreach(Vector3 step in originalPath)
        {
            if (Methods.IsEmptyGrid(step))
            {
                returnList.Add(step);
            }
        }
        return returnList;
    }

    static public List<Vector3> GetPath(Vector3 anchorA, Vector3 anchorB)
    {
        // returns path from AnchorA to AnchorB, which may include reds, empty grids, anchor locations and start and end points.
        // (Filtered at GetAdjacents)
        // returns empty list if search fails.

        //floor to grid locations, in case not already corrected
        anchorA.x = Mathf.Floor(anchorA.x);
        anchorA.y = Mathf.Floor(anchorA.y);
        anchorB.x = Mathf.Floor(anchorB.x);
        anchorB.y = Mathf.Floor(anchorB.y);

        List<Vector3> path = new List<Vector3>();
        List<SearchNode> openList = new List<SearchNode>();
        Dictionary<Vector3, SearchNode> closedList = new Dictionary<Vector3, SearchNode>();
        List<Vector3> adjacents;
        SearchNode current, addNode;
        Vector3 point;
        float g, h, f;

        h = Vector3.Distance(anchorA, anchorB);
        addNode = new SearchNode(anchorA, null, 0, h);   //f=h when g=0
        openList.Add(addNode);
        while (openList.Count > 0)
        {
            //pop first in list
            openList.Sort(SearchNode.CompareNodes);
            current = openList[0];
            openList.RemoveAt(0);

            if (closedList.ContainsKey(current.position))
            {
                continue;
            }
            closedList.Add(current.position, current);

            if (current.position == anchorB)
            {
                //reached goal
                point = anchorB;
                path.Add(anchorB);
                //assemble path
                while (point != anchorA)
                {
                    current = current.parent;
                    point = current.position;
                    path.Insert(0, point);  //insert at front
                }
            }

            adjacents = GetAdjacents(current.position);

            foreach (Vector3 node in adjacents)
            {
                //cost so far plus cost from here to there
                if (Methods.IsEmptyGrid(node))
                {
                    g = current.g + 1;
                }
                else
                {
                    g = current.g + 0.5f; // Vector3.Distance(current.position, node);
                }                    
                f = g + Vector3.Distance(node, anchorB);
                addNode = new SearchNode(node, current, g, f);
                if (closedList.ContainsKey(node))
                {
                    //if this node is on the closed list but with a higher cost, swap it out
                    if (closedList[node].f > f)
                    {
                        closedList[node] = addNode;
                    }
                }
                else
                {
                    openList.Add(addNode);
                }
            }
        }
        return path;
    }

    static public int GetStepsRemaining(Vector3 anchorA, Vector3 anchorB)
    {
        int stepCount = 0;
        List<Vector3> path = GetPath(anchorA, anchorB);

        foreach (Vector3 step in path)
        {
            if (Methods.IsEmptyGrid(step))
            {
                stepCount++;
            }
        }
        return stepCount;
    }

    //returns valid path adjacents - either empty, red or on anchor
    private static List<Vector3> GetAdjacents(Vector3 pos)
    {
        int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, 1, -1 };
        Vector3 adj;

        List<Vector3> returnList = new List<Vector3>();

        for (int i = 0; i < GameParameters.instance.searchDirections; i++)//neighbours
        {
            adj = new Vector3(pos.x + dx[i], pos.y + dy[i], 0f);
            if (Methods.IsOnBoard(adj) && (Methods.OnCounter(adj) == Constants.RED || Methods.IsEmptyGrid(adj) || Methods.GetAnchorCenter(adj) != Vector3.zero))
                returnList.Add(adj);
        }

        return returnList;
    }
}
