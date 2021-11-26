using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using System.Reflection;

public class OptionMenu : MonoBehaviour
{

    private List<string> modes;
    private List<string> agents;
    private List<string> blockers;

    public Dropdown modeDropdown;
    public Dropdown agentDropdown;
    public Dropdown blockerDropdown;
    public Dropdown diagDropdown;
    public Dropdown faceDownDropdown;
    public Dropdown convincersDropdown;
    public InputField shuttleNumIF;
    public InputField gridSizeIF;
    public InputField carryLimitIF;
    public InputField anchorNumIF;
    public InputField counterNumIF;
    public InputField minDisIF;
    public InputField cusAnchorPosIF;
    public InputField tokenColorIF;	
    public InputField blocksPerTurnIF;
    public InputField timeLimitIF;
    public Text errorText;
    public Text successText;

    private int tempMode, tempAgent, tempBlocker, tempDiags;
  
    private List<Vector3> cusAnchors = new List<Vector3>();

    public void modeDropdown_IndexChanged(int index)
	{
        tempMode = index;//only process dropdown selections if applied.
	}

    public void agentDropdown_IndexChanged(int index)
    {
        tempAgent = index;
    }

    public void blockerDropdown_IndexChanged(int index)
    {
        tempBlocker = index;
    }

    public void diagsDropdown_IndexChanged(int index)
    {
        tempDiags = index;
    }

    public void convincersDropdown_IndexChanged(int index)
    {
        if (index == Constants.YES)
            GameParameters.instance.convincers = true;
        else
            GameParameters.instance.convincers = false;
    }

    public void faceDownDropdown_IndexChanged(int index)
    {
        if (index == Constants.YES)
            GameParameters.instance.faceDown = true;
        else
            GameParameters.instance.faceDown = false;
    }


    void OnEnable()
    {
        PopulateOptions();
    }


    //Populate options menu from game parameters
    void PopulateOptions()
	{

		shuttleNumIF.text = GameParameters.instance.shuttleNum.ToString();
	    gridSizeIF.text = GameParameters.instance.gridSize.ToString();
	    carryLimitIF.text = GameParameters.instance.carryLimit.ToString();
	    anchorNumIF.text = GameParameters.instance.anchorCount.ToString();
	    //counterNumIF.text = GameParameters.instance.counterNumInGenerator.ToString();
	    minDisIF.text = GameParameters.instance.minAnchorDis.ToString();
	    tokenColorIF.text = GameParameters.instance.tokenColors.ToString();
	    blocksPerTurnIF.text = GameParameters.instance.blocksPerTurn.ToString();
        timeLimitIF.text = GameParameters.instance.timeLimit.ToString();//CHANGED

        if (GameParameters.instance.searchDirections == 8)
        {
            diagDropdown.value = Constants.YES;
        }
        else
        {
            diagDropdown.value = Constants.NO;
        }

        if (GameParameters.instance.convincers == true)
        {
            convincersDropdown.value = Constants.YES;
        }
        else
        {
            convincersDropdown.value = Constants.NO;
        }

        if (GameParameters.instance.faceDown == true)
        {
            faceDownDropdown.value = Constants.YES;
        }
        else
        {
            faceDownDropdown.value = Constants.NO;
        }

        agentDropdown.ClearOptions();
        agentDropdown.AddOptions(GetAgents());
        blockerDropdown.ClearOptions();
        blockerDropdown.AddOptions(GetBlockers());
        Debug.Log("Selected agent is: " + GameParameters.instance.selectedAgent);
        agentDropdown.value = agents.IndexOf(GameParameters.instance.selectedAgent);
        blockerDropdown.value = blockers.IndexOf(GameParameters.instance.selectedBlocker);
        modeDropdown.ClearOptions();
        modeDropdown.AddOptions(GetModes());
        modeDropdown.value = GameParameters.instance.mode;

        Debug.Log("Options copied from game parameters.");
	}

    private List<string> GetModes()
    {
        modes = new List<string>() { "Immediate", "Interrupted", "Online" };
        return modes;
    }

    private List<string> GetAgents()
    {
        agents= AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                 .Where(x => typeof(IPlayerA).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                 .Select(x => x.Name).ToList();
        return agents;
    }

    private List<string> GetBlockers()
    {
        blockers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
             .Where(x => typeof(IPlayerB).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
             .Select(x => x.Name).ToList();
        blockers.Insert(0, "Human");
        return blockers;
    }



	public void SetMoreOptions()
    {
        errorText.text = "";
        successText.text = "";
        if (CheckInputValid())
        {
            GameParameters.instance.shuttleNum = StringToInt(shuttleNumIF.text);
            GameParameters.instance.gridSize = StringToInt(gridSizeIF.text);
            GameParameters.instance.carryLimit = StringToInt(carryLimitIF.text);
            GameParameters.instance.anchorCount = StringToInt(anchorNumIF.text);
            //GameParameters.instance.counterNumInGenerator = StringToInt(counterNumIF.text);
            GameParameters.instance.minAnchorDis = StringToInt(minDisIF.text);
            GameParameters.instance.tokenColors = StringToInt(tokenColorIF.text);
            GameParameters.instance.timeLimit = StringToFloat(timeLimitIF.text);
            GameParameters.instance.blocksPerTurn = StringToInt(blocksPerTurnIF.text);
            
            if (GameParameters.instance.tokenColors <3)
			{
				tokenColorIF.text = "2";
				GameParameters.instance.SetColorProportion(50, 0, 50);
			}
			else{
				tokenColorIF.text = "3";
                GameParameters.instance.SetColorProportion(40, 30, 30);
            }
            
            if (cusAnchors.Count > 0)
            {
                GameParameters.instance.randomAnchor = false;
                foreach (Vector3 pos in cusAnchors)
                {
                    GameParameters.instance.defaultAnchorPos.Add(pos);
                }
            }

            if (tempDiags == Constants.YES)
            {
                GameParameters.instance.searchDirections = 8;
            }
            else
            {
                GameParameters.instance.searchDirections = 4;
            }
            Debug.Log("searchDirections set to " + GameParameters.instance.searchDirections);

            GameParameters.instance.SetMode(tempMode);
            Debug.Log("Mode: " + modes[tempMode]);
            GameParameters.instance.SetAgent(agents[tempAgent]);
            Debug.Log("PlayerA: " + agents[tempAgent]);
            GameParameters.instance.SetBlocker(blockers[tempBlocker]);
            Debug.Log("PlayerB: " + blockers[tempBlocker]);

            StartCoroutine(ShowDoneMessage());
        }
        else
        {
            errorText.color = Color.red;
            errorText.text = "Invalid Parameters! Please check and try again.";
        }
    }

    IEnumerator ShowDoneMessage()
    {
        successText.color = Color.black;
        successText.text = "Saved. Enjoy your game!";
        yield return new WaitForSeconds(1.5f);
        successText.text = "";
        gameObject.SetActive(false);
    }



    private bool CheckInputValid()
    {
        if (!(StringToInt(shuttleNumIF.text) >= 1 && StringToInt(shuttleNumIF.text) <= 4))
        {
            return false;
        }
        if (!(StringToInt(gridSizeIF.text) >= 20 && StringToInt(gridSizeIF.text) <= 40))
        {
            return false;
        }
        if (!(StringToInt(carryLimitIF.text) >= 1 && StringToInt(carryLimitIF.text) <= 4))
        {
            return false;
        }
        if (StringToInt(anchorNumIF.text) < 2)
        {
            return false;
        }
        if (!(StringToInt(counterNumIF.text) >= 1 && StringToInt(counterNumIF.text) <= 9))
        {
            return false;
        }
        if (StringToInt(minDisIF.text) < 3)
        {
            return false;
        }
        cusAnchors.Clear();
        if (cusAnchorPosIF.text.Length > 0)
        {
            string[] positions = cusAnchorPosIF.text.Split(' ');
            // Trans string to Vector3
            for (int i = 0; i < positions.Length; i++)
            {
                // Remove '(' and ')'
                positions[i] = positions[i].Substring(1, positions[i].Length - 2);
                string[] numbers = positions[i].Split(',');
                // Check if the numbers end by ".5"
                if (numbers.Length != 2 || numbers[0][numbers[0].Length - 1] != '5' || numbers[1][numbers[1].Length - 1] != '5' || numbers[0][numbers[0].Length - 2] != '.' || numbers[1][numbers[1].Length - 2] != '.')
                {
                    return false;
                }
                float x = StringToFloat(numbers[0]);
                float y = StringToFloat(numbers[1]);
                if (!Mathf.Approximately(x, -1) && !Mathf.Approximately(y, -1) && x >= 0.5f && y >= 0.5f && x <= StringToInt(gridSizeIF.text) - 1.5f && y <= StringToInt(gridSizeIF.text) - 1.5f)
                {
                    cusAnchors.Add(new Vector3(x, y, 0f));
                }
                else
                {
                    return false;
                }
            }

            // Check minimal distance between two anchors >= 2
            for (int i = 0; i < cusAnchors.Count; i++)
            {
                for (int j = i+1; j < cusAnchors.Count; j++)
                {
                    if (Vector3.Distance(cusAnchors[i], cusAnchors[j]) < 2)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private int StringToInt(string s)
    {
        int num = -1;
        try
        {
            num = Int32.Parse(s);
            return num;
        }
        catch (FormatException)
        {
            return -1;
        }
    }

    private float StringToFloat(string s)
    {
        float num = -1;
        try
        {
            num = float.Parse(s);
            return num;
        }
        catch (FormatException)
        {
            return -1;
        }
    }
}
