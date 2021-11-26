using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Calling program runs decayAll once for all regions.
//Then, for each Player A move, checks it's InRegion, then either executes NewPlacement or NewConvincer.
//To decide most suspicious region, calling program seeks max density (disambiguate on contiguity) redZone.
//Runs CalcRedZone to get proportion by which remembered reds dominate remembered nonreds. 

public class Region 
    
{
    public static int predecay = 3;//how many turns to remember

    private Vector3 anchorA, anchorB;
    private int size;
    private int pop=0; //population
    private double density = 0f;
    private int contiguity = 0;
    //remembered
    private List<int> reds = new List<int>();
    private List<int> nonreds = new List<int>();


    public Region(Vector3 A, Vector3 B)
    {
        anchorA = A;
        anchorB = B;

        int minX = Mathf.Min(Mathf.FloorToInt(anchorA.x), Mathf.FloorToInt(anchorB.x));
        int maxX = Mathf.Max(Mathf.CeilToInt(anchorA.x), Mathf.CeilToInt(anchorB.x));
        int minY = Mathf.Min(Mathf.FloorToInt(anchorA.y), Mathf.FloorToInt(anchorB.y));
        int maxY = Mathf.Max(Mathf.CeilToInt(anchorA.y), Mathf.CeilToInt(anchorB.y));

        size = (maxX - minX+1) * (maxY - minY+1) - 8; //subtract cells taken up by anchors.
        //Debug.Log("Region size between (" + anchorA.x + "," + anchorA.y + ") and (" + anchorB.x + "," + anchorB.y + "): " + size);

    }

    public bool InRegion(Vector3 token)
    {
        return Methods.IsInner(token, new List<Vector3> { anchorA, anchorB });
    }

    public void NewPlacement(Vector3 token)
    {
        //Debug.Log("Processing placement in region between " + anchorA.x + "," + anchorA.y + " and " + anchorB.x + "," + anchorB.y);
        pop++;
        //Debug.Log("pop: " + pop + "; size: " + size + "; density: " + (double)pop/(double)size);
        density = (double)pop / (double)size;
       
        List<Vector3> adjList = GetAdjacents(token);
        foreach (Vector3 adj in adjList)
        {
            if (Methods.OnCounter(adj) > -1)
            {
                contiguity++;
            }
        }
        BoostMemory(token, false);
    }

    public void NewConvincer(Vector3 token)
    {
        BoostMemory(token, true);//only consider nonred convincers, not placements
    }


    public void decayAll()
    {
        for (int i = 0; i < reds.Count; i++)
        {
            reds[i]--;
        }
        reds.RemoveAll(isZero);

        for (int i = 0; i < nonreds.Count; i++)
        {
            nonreds[i]--;
        }
        nonreds.RemoveAll(isZero);
    }

    //value to determine zone redness
    public double CalcRedZone()
    {
        //return (reds.Count - nonreds.Count)/size;
        //return reds.Count / size;
        return (reds.Count-nonreds.Count) / Vector3.Distance(anchorA, anchorB);
    }

    private bool isZero(int val)
    {
        return val == 0;
    }

    public double GetDensity()
    {
        return density;
    }

    public int GetContiguity()
    {
        return contiguity;
    }

    public Vector3[] GetAnchors()
    {
        Vector3[] anchors = {anchorA, anchorB};
        return anchors;
    }

    private void BoostMemory(Vector3 token, bool convincer)
    {
        if (Methods.OnCounter(token) == Constants.RED)
        {
            reds.Add(predecay);
            if (convincer) reds.Add(predecay);//double up
            //Debug.Log("number of reds: "+reds.Count);
        }
        else if (convincer)//convincers only
        {
            nonreds.Add(predecay);
            //Debug.Log("number of nonreds: " + nonreds.Count);
        }
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

    public List<Vector3> GetAllGridLocations()
    {
        List<Vector3> gridLocs = new List<Vector3>();
        //generate complete list of grid locations
        int minX = Mathf.Min(Mathf.FloorToInt(anchorA.x), Mathf.FloorToInt(anchorB.x));
        int maxX = Mathf.Max(Mathf.CeilToInt(anchorA.x), Mathf.CeilToInt(anchorB.x));
        int minY = Mathf.Min(Mathf.FloorToInt(anchorA.y), Mathf.FloorToInt(anchorB.y));
        int maxY = Mathf.Max(Mathf.CeilToInt(anchorA.y), Mathf.CeilToInt(anchorB.y));
            
        for (int i=minX; i<=maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                gridLocs.Add(new Vector3((float)i, (float)j, 0f));
            }
        }

        return gridLocs;
    }
}
