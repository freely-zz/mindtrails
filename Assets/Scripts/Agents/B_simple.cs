using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class B_Simple : IPlayerB
{
    private int gridSize = Config.gridSize;

    public List<Vector3> DecideBlocks(List<Actions> AIactions, List<Vector3> convincers, float timeRemaining)
    {
        List<Vector3> blocks = new List<Vector3>();
        List<Vector3> reds = new List<Vector3>();
        int i;
        Vector3 tempBlock = new Vector3();
            
        //parse actions to extract deposited tokens
        List<Vector3> placements = new List<Vector3>();
        foreach (Actions acts in AIactions)
        {
            placements.AddRange(GetPlacements(acts));
        }

        //get all the reds from last turn.
        foreach (var b in placements)
        {
            //red is 0.
            if (Methods.OnCounter(b) == 0)
            {
                reds.Add(b);
            }
        }

        for (i=0; i<Config.blocksPerTurn; i++)
        {
            tempBlock = placeNextTo(reds);
            while (blocks.Contains(tempBlock) || !Methods.IsEmptyGrid(tempBlock))
            {
                tempBlock = placeNextTo(reds);
            }
            blocks.Add(tempBlock);
        }

        foreach (var b in blocks)
        {
            UnityEngine.Debug.Log(b);
        }

        return blocks;
    }

    private List<Vector3> GetPlacements(Actions actions)
    {
        List<Vector3> placements = new List<Vector3>();

        for (int i = 0; i < actions.commands.Count; i++)
        {
            string[] commands = actions.commands[i].Split('#');
            //if token deposited - include and not moved?
            if (commands[0] == "Deposit") // && !Methods.IsEmptyGrid(actions.paras[i]))
            {
                placements.Add(actions.paras[i]);

            }
        }
        return placements;
    }

    //place block next to location block
    //TODO shorten this
    public Vector3 placeNextTo(List<Vector3> reds)
    {
        //makeshift solution for infinite loop prevention
        //TODO investigate
        int check = 0;
        List<int> next = new List<int>() { -1, 0, 1 };
        Vector3 block = reds[Random.Range(0, reds.Count)];
        int dx = next[Random.Range(0, next.Count)];
        int dy = next[Random.Range(0, next.Count)];

        Vector3 placement = new Vector3(block[0] + dx, block[1] + dy, 0);

        while (((dx == 0 && dy == 0) || blockOnAnchor(placement) || !Methods.IsEmptyGrid(placement)) && check < 10)
        {
            block = reds[Random.Range(0, reds.Count)];
            dx = next[Random.Range(0, next.Count)];
            dy = next[Random.Range(0, next.Count)];
            placement = new Vector3(block[0] + dx, block[1] + dy, 0);
            check++;
        }

        if (check > 10)
        {
            return Methods.GetRandomEmptyGrid(1)[0];
        }

        return placement;
    }

    //make sure not blocking on an anchor
    public bool blockOnAnchor(Vector3 block)
    {
        List<Vector3> allAnchors = Methods.GetAllAnchors();
        foreach (var anchor in allAnchors)
        {
            //distance too close to anchor
            if (Mathf.Abs(block[0] - anchor[0]) == 0.5 || Mathf.Abs(block[1] - anchor[1]) == 0.5)
            {
                return true;
            }
        }

        return false;
    }
}
