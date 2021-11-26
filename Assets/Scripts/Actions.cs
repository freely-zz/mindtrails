/*
 *  Returned to GameManager by agent for each shuttle. 
 *  Actions class assembles list of commands (actions) the shuttle will take sequentially in this turn, 
 *  which may include waiting, changes of speed, etc. 
 */

using System.Collections.Generic;
using UnityEngine;

public class Actions
{
   
    public List<string> commands = new List<string>();
    public List<Vector3> paras = new List<Vector3>();

    private List<int[]> carry = new List<int[]>();//how many of each color currently carried, by shuttle
    private List<int[]> bagCounterColor = new List<int[]>();//color at each position or -1, by shuttle
    private Dictionary<Vector2, List<int>> deposits = new Dictionary<Vector2, List<int>>();//deposits made during actions

    private int currentShuttle = 0;



    //=========================================================================================================
    //   ***************************************CORE ACTIONS***********************************************
    //=========================================================================================================

    //Collects specified number of tokens from nominated generator via CollectAt()
    public void CollectFromGen(int generatorId, int num)
    {
        Vector3 pos;
        List<Vector3> tokenPositions = new List<Vector3>();
        GeneratorManager gmScriptRef = GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>();

        pos = GameManager.instance.GetParkingPosition(generatorId);
        MoveTo(pos);
        tokenPositions = gmScriptRef.GetPickupPositions();


        if (num > GameParameters.instance.carryLimit) num = GameParameters.instance.carryLimit;//don't be silly
  

        for (int i = 0; i < num; i++)
        {
            CollectAt(tokenPositions[i]);
        }
        
    }

    // Collects a counter from the board
    public void CollectFromGrid(Vector3 pos)
    {
        int color = GameManager.instance.deposited[(int)pos.x][(int)pos.y];
        if (color == -1)
        {
            color = CollectDeposited(pos);
        }
        if (color != -1)//because deposited during these actions
        {
            MoveTo(pos);
            commands.Add("CollectFromGrid");
            paras.Add(pos);
            UpdateCarryAndBagForCollect(color);
        }
    }


    // Deposits counter from next filled shuttle slot via DepositIndexAt()
    public void DepositAt(Vector3 pos)
    {      
        MoveTo(pos);
        int[] bagcontent = GetPickupColorBagPos();
        for (int i = 0; i < bagcontent.Length; i++)
        {
            if (bagCounterColor[currentShuttle][i] != -1)
            {
                LogDeposit(pos, bagCounterColor[currentShuttle][i]);
                DepositIndexAt(pos, i);
                break;
            }
        }
    }

    //Moves off grid at current y pos
    public void Park()
    {
        commands.Add("Park");
        paras.Add(new Vector3(0f, 0f, 0f));//included to maintain index
    }

    //waits w seconds
    public void Wait(float w)
	{
		commands.Add("Wait");
		paras.Add(new Vector3(0f,0f,w));
	}

	// Sets speed 
	public void Speed(float s)
	{
		commands.Add("Speed");
		paras.Add(new Vector3(0f,0f,s));
	}

    // Moves to pos
    public void MoveTo(Vector3 pos)
    {
        commands.Add("Move"); 
        paras.Add(pos);
    }

    // Moves to each pos in list (so can describe route for convincers or to confse)
    public void MoveTo(List<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            commands.Add("Move");
            paras.Add(pos);
        }
    }


    //HELPER METHODS
    // Gets nos of red, yellow and blue counters in shuttle
    public int[] GetPickupColor()
    {
        if (carry.Count == 0)
        {
            return new int[] { 0, 0, 0 };
        }
        return carry[carry.Count - 1];
    }

    // Gets the color of counter on each bag position, returns -1 if index is empty
    public int[] GetPickupColorBagPos()
    {
        if (bagCounterColor.Count == 0)
        {
            return new int[] { -1, -1, -1, -1 };
        }
        return bagCounterColor[bagCounterColor.Count - 1];
    }

    private bool IsRoomInShuttle()
    {
        for (int i = 0; i < bagCounterColor[currentShuttle].Length; i++)
        {
            if (bagCounterColor[currentShuttle][i] == -1)
            {
                return true;
            }
        }
        return false;
    }


    //=========================================================================================================
    // VARIATIONS ON COLLECT/DEPOSIT - ASSUME YOU HAVE EXECUTED MoveTo() AND ARE IN POSITION.
    //=========================================================================================================

    // Collects a counter from pos
    public void CollectAt(Vector3 pos)
    {
        commands.Add("Collect");
        paras.Add(pos);
        int id = Methods.OnPickup(pos);
        int color = GameManager.instance.generators[id].GetComponent<GeneratorManager>().GetPickupsInGnColor(pos);
        UpdateCarryAndBagForCollect(color);
    }

    // Deposits counter - by colour - at the grid position
    public void DepositAt(Vector3 pos, int color)
    {
        MoveTo(pos);
        commands.Add("Deposit#Color#" + color.ToString());
        paras.Add(new Vector3(pos.x, pos.y, 0f));
        UpdateCarryAndBagForDeposit(color);
    }

    // Deposits counter - by colour - at the position with specified delay
    public void DepositAt(Vector3 pos, int color, float delay)
    {
        commands.Add("Deposit#Color#" + color.ToString());
        // (x,y), z == delay
        paras.Add(new Vector3(pos.x, pos.y, delay));
        UpdateCarryAndBagForDeposit(color);
    }

    // Deposits counter - by index in shuttle - at the grid position - called by base DepositAt();
    public void DepositIndexAt(Vector3 pos, int index)
    {
        commands.Add("Deposit#Index#" + index.ToString());
        paras.Add(new Vector3(pos.x, pos.y, 0f));
        //AddNewCarryAndBag();
        carry[currentShuttle][bagCounterColor[currentShuttle][index]]--;
        bagCounterColor[currentShuttle][index] = -1;
    }

    // Deposits counter - by index in shuttle - at the grid position with specified delay
    public void DepositIndexAt(Vector3 pos, int index, float delay)
    {
        commands.Add("Deposit#Index#" + index.ToString());
        // (x,y), z == delay
        paras.Add(new Vector3(pos.x, pos.y, delay));
        carry[carry.Count - 1][bagCounterColor[bagCounterColor.Count - 1][index]]--;
        bagCounterColor[bagCounterColor.Count - 1][index] = -1;
    }



    //=========================================================================================================
    // MOSTLY SYSTEM - SOME DEPRECATED
    //=========================================================================================================
    // Returns a list of positions that have been added collect commands in this actions
    private List<Vector3> GetCollectPosFromActions(Actions acs)
    {
        List<Vector3> collectList = new List<Vector3>();
        for (int i = 0; i < acs.commands.Count; i++)
        {
            if (acs.commands[i].Length >= 7 && acs.commands[i].Substring(0, 7).Equals("Collect"))
            {
                collectList.Add(new Vector3(acs.paras[i].x, acs.paras[i].y, 0f));
            }
        }
        return collectList;
    }

    // Returns a list of positions that have been added deposit commands in this actions as well as all other shuttles' actions.
    public List<Vector3> GetDepositPos(List<Actions> AIactions)
    {
        List<Vector3> depositList = new List<Vector3>();
        depositList.AddRange(GetDepositPosFromActions(this));
        for (int i = 0; i < AIactions.Count; i++)
        {
            depositList.AddRange(GetDepositPosFromActions(AIactions[i]));
        }
        return depositList;
    }

    // Returns a list of positions that have been added collect commands in this actions as well as all other shuttles' actions.
    public List<Vector3> GetCollectPos(List<Actions> AIactions)
    {
        List<Vector3> collectList = new List<Vector3>();
        collectList.AddRange(GetCollectPosFromActions(this));
        for (int i = 0; i < AIactions.Count; i++)
        {
            collectList.AddRange(GetCollectPosFromActions(AIactions[i]));
        }
        return collectList;
    }

    // Returns a list of positions that have been added deposit commands in this actions
    public List<Vector3> GetDepositPosFromActions(Actions acs)
    {
        List<Vector3> depositList = new List<Vector3>();
        for (int i = 0; i < acs.commands.Count; i++)
        {
            if (acs.commands[i].Length > 7 && acs.commands[i].Substring(0, 7).Equals("Deposit"))
            {
                depositList.Add(new Vector3(acs.paras[i].x, acs.paras[i].y, 0f));
            }
        }
        return depositList;
    }

    // Turns over the counter with the given index
    public void TurnOverCounterInBagByIndex(int index)
    {
        commands.Add("TurnOver#" + index.ToString());
        paras.Add(new Vector3(0f, 0f, 0f));
    }

    private void InitializeCarryAndBag()
    {
        carry.Add(new int[3]);
        bagCounterColor.Add(new int[] { -1, -1, -1, -1 });
    }

    private void CopyTheLastCarryAndBag()
    {
        InitializeCarryAndBag();
        carry[carry.Count - 2].CopyTo(carry[carry.Count - 1], 0);
        bagCounterColor[bagCounterColor.Count - 2].CopyTo(bagCounterColor[bagCounterColor.Count - 1], 0);
    }

    private void AddNewCarryAndBag()
    {
        if (carry.Count == 0)
        {
            InitializeCarryAndBag();
        }
        else
        {
            CopyTheLastCarryAndBag();
        }
    }


    //maintain record of colours carried after collection
    private void UpdateCarryAndBagForCollect(int color)
    {
        AddNewCarryAndBag();
        carry[currentShuttle][color]++;
        for (int i = 0; i < bagCounterColor[currentShuttle].Length; i++)
        {
            if (bagCounterColor[currentShuttle][i] == -1)
            {
                bagCounterColor[currentShuttle][i] = color;
                break;
            }
        }
    }

    //maintain record of colours carried after deposit
    private void UpdateCarryAndBagForDeposit(int color)
    {
        AddNewCarryAndBag();
        carry[currentShuttle][color]--;
        for (int i = 0; i < bagCounterColor[currentShuttle].Length; i++)
        {
            if (bagCounterColor[currentShuttle][i] == color)
            {
                bagCounterColor[currentShuttle][i] = -1;
                break;
            }
        }
    }



    private void LogDeposit(Vector3 pos, int color)
    {
        Vector2 location = new Vector2(pos.x, pos.y);
        if (!deposits.ContainsKey(location))
        {
            List<int> colorlist = new List<int>();
            colorlist.Add(color);
            deposits.Add(location, colorlist);
        }
        else
        {
            deposits[location].Add(color);
        }

        foreach (int col in deposits[location])
        {
            //Debug.Log("depositing " + col);
        }

        return;
    }

    private int CollectDeposited(Vector3 pos)
    {
        Vector2 location = new Vector2(pos.x, pos.y);
        if (!deposits.ContainsKey(location))
        {
            return -1;
        }
        else
        {
            foreach (int col in deposits[location])
            {
                //Debug.Log("collecting" + col);
            }

            int numDeposits = deposits[location].Count;
            int color = deposits[location][numDeposits - 1];
            if (numDeposits == 1)
            {
                deposits.Remove(location);
            }
            else
            {
                deposits[location].RemoveAt(numDeposits - 1);
            }
            return color;
        }

    }

}
