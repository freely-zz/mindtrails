using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B_Auto : IPlayerB
{
    private List<Region> regions = new List<Region>();
    private int memoryMargin = 1; //number of remembered reds that need to be exceeded to select redZone over denseZone.

    public List<Vector3> DecideBlocks(List<Actions>AIactions, List<Vector3> convincers, float timeRemaining)
    {
        //perform once, first time decideblocks is called.
        if (regions.Count == 0) PopulateRegions();

        //housekeeping
        Methods.UnhighlightCells();
        foreach (Region r in regions)
        {
            r.decayAll();//performed once on each turn for all regions
        }

        //parse actions to extract deposited tokens
        List<Vector3> placements = new List<Vector3>();
        foreach (Actions acts in AIactions)
        {
            placements.AddRange(GetPlacements(acts));
        }
       
        //process placements according to region
        foreach(Vector3 placement in placements)
        {
            foreach(Region r in regions)
            {
                if (r.InRegion(placement)){
                    r.NewPlacement(placement);
                }
            }
        }

        foreach(Vector3 convincer in convincers)
        {
            foreach (Region r in regions)
            {
                if (r.InRegion(convincer))
                {
                    r.NewConvincer(convincer);
                }
            }
        }

        Region selection = DecideRegion();
        //show where you would block
        Methods.HighlightCells(selection.GetAllGridLocations());
        
        //appropriate action tbd, meanwhile block anywhere in that region!
        return Methods.GetRandomEmptyGrid(Config.blocksPerTurn, selection.GetAnchors()[0], selection.GetAnchors()[1]);
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

    private void PopulateRegions()
    {
        //Debug.Log("Populating Regions");
        List<Vector3> anchors = Methods.GetAllAnchors();
        for(int i = 0; i < anchors.Count - 1; i++)
        {
            for(int j = i + 1; j < anchors.Count; j++)
            {
                regions.Add(new Region(anchors[i], anchors[j]));
            }
                
        }
        //Debug.Log(regions.Count + " regions added");

    }

    private Region DecideRegion()
    {
        //identify max density redzone.
        double maxDensity = 0f;
        int denseReg = 0;
        double maxReds = 0;
        int redReg = 0;
        double redZone;

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].GetDensity() > maxDensity)
            {
                maxDensity = regions[i].GetDensity();
                denseReg = i;
            }
            redZone = regions[i].CalcRedZone();//amount reds more than nonreds
            if (redZone > maxReds)
            {
                maxReds = redZone;
                redReg = i;
            }
        }

        Debug.Log("max density is " + maxDensity);
        Debug.Log("maxReds is " + maxReds);

        if (denseReg == redReg)
        {
            return regions[denseReg];
        }
        else
        {
            //densest region not reddest region. Decide based on contiguity
            int contiguityDiff = regions[denseReg].GetContiguity() - regions[redReg].GetContiguity();
            if (contiguityDiff > 0)
            {
                return regions[denseReg];
            }
            else if (contiguityDiff < 0)
            {
                return regions[redReg];
            }
            Debug.Log("no contiguity diff");
            //if no difference, decide based on redness > margin
            double densityDiff = maxDensity - regions[redReg].GetDensity();
            double redDiff = maxReds - regions[denseReg].CalcRedZone();
            Debug.Log("red region dominates by " + redDiff);
            if (redDiff > memoryMargin)
            {
                return regions[redReg];
            }
            else return regions[denseReg];
        }
    }
}