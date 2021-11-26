/*
 * GameParameters control the parameters of the game. 
 * If you make selections in the "Options" menu, it will override the values in GameParameters.
 */

using System.Collections.Generic;
using UnityEngine;

public class GameParameters : MonoBehaviour
{
    public static GameParameters instance;


    
    public int shuttleNum;// The number of the shuttles
    public int gridSize;    // The default size of the board
    public float minAnchorDis;    // The minimal distance between two anchors
    public int counterNumInGenerator;   // The number of counters in each generator
    public int carryLimit;    // The number of counters each shuttle can carry
    public int anchorCount;   // The number of anchors
    public bool randomAnchor;   // The positions of the anchors are random or default
    public float timeLimit;
    public int tokenColors;
	public int blocksPerTurn;
	public bool pauseBeforeAgent;
	public int mode; //0=immediate, 1=interrupted, 2=online
	public bool convincers;
    public bool faceDown;
	public bool fakeOnline;

	//public int randomDistrib;
    public string selectedAgent;
    public string selectedBlocker;
    public int searchDirections = 8;

    public bool diags;

	public Vector3[] regionAnchors = new Vector3[2];//to pass from agent to AIBehavior

    public List<Vector3> defaultAnchorPos;

    // Uses to control the proportion of each color of apples, default is equal proportion
    [HideInInspector] public List<int> colorBag;

	private void LoadParams()
	{
		shuttleNum = Config.shuttleNum;
		gridSize = Config.gridSize;
		minAnchorDis = Config.minAnchorDis;

		carryLimit = Config.carryLimit;
		anchorCount = Config.anchorCount;

        timeLimit = Config.timeLimit;
		tokenColors = Config.tokenColors;
		blocksPerTurn = Config.blocksPerTurn;
        if (Config.diags)
        {
            searchDirections = 8;
        }
        else
        {
            searchDirections = 4;
        }

        if (tokenColors < 3) SetColorProportion(50, 0, 50);
        SetMode(Config.mode);
        SetAgent(Config.selectedAgent);
        SetBlocker(Config.selectedBlocker);

        convincers = Config.convincers;
        faceDown = Config.faceDown;


        //hardcoded (not set in config)
        randomAnchor = true;
        //convincers = true;
        counterNumInGenerator = 4;
        //fakeOnline = Config.fakeOnline;
        //usedAgents = Config.usedAgents;
        //pauseBeforeAgent = Config.pauseBeforeAgent;
    }

    private void SaveParams()
	{
		Config.shuttleNum = shuttleNum;
		Config.gridSize = gridSize;
		Config.minAnchorDis = minAnchorDis;
		//Config.counterNumInGenerator = counterNumInGenerator;
		Config.carryLimit = carryLimit;
		Config.anchorCount = anchorCount;
        //Config.randomAnchor = randomAnchor;
        Config.timeLimit = timeLimit;
		Config.tokenColors = tokenColors;
		Config.blocksPerTurn = blocksPerTurn;
		//Config.pauseBeforeAgent = pauseBeforeAgent;
		Config.mode = mode;
		Config.convincers = convincers;
        Config.faceDown = faceDown;
        Config.selectedAgent = selectedAgent;
        Config.selectedBlocker = selectedBlocker;
        if (searchDirections == 8)
        {
            Config.diags = true;
        }
        else
        {
            Config.diags = false;
        }

        Config.faceDown = faceDown;
        //Config.fakeOnline = fakeOnline;
        //Config.usedAgents = usedAgents;
    }

    private void Awake()
    {
        if (instance == null)
        {
			//DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
			
        }

        SetCustomAnchorPos();
        InitializeColorProportion();
        SetDefaultColorProportion();
		LoadParams();
    }

	//online or offline functionality - accessed from option menu
	public void SetMode(int num){
		mode = num;
	}

    public void SetAgent(string name)
    {
        selectedAgent = name;
    }

    public void SetBlocker(string name)
    {
        selectedBlocker = name;
    }

    private void OnDestroy()
	{
		SaveParams();
        Config.gameParamString = "\n****Config Settings***\n" + ToString();
        
	}

    // Edit this function to set customized anchor positions
    private void SetCustomAnchorPos()
    {
        defaultAnchorPos.Clear();
        // Add Postions to defaultAnchorPos here to customize Anchors' Positions
        // Keep empty if you don't need to customize Anchors' Positions
        // You need to select "Default" in Options Menu 
        /* Example : Anchor Positions are (1.5, 1.5), (4.5, 4.5), (6.5, 6.5), (8.5, 8.5)
        defaultAnchorPos.Add(new Vector3(1.5f, 1.5f, 0f));
        defaultAnchorPos.Add(new Vector3(4.5f, 4.5f, 0f));
        defaultAnchorPos.Add(new Vector3(6.5f, 6.5f, 0f));
        defaultAnchorPos.Add(new Vector3(8.5f, 8.5f, 0f));
        */
        // Your code BEGINS HERE


        // Your code ENDS HERE
    }

    // Edit this function to initialize the proportions of each colored counter in the generators
    private void InitializeColorProportion()
    {
        colorBag.Clear();
        // Initialize colors' proportions for generators here
        // Keep empty if you don't need to customize
        /* Example : 100% red, 0% yellow and 0% blue
        SetColorProportion(100, 0, 0);
        */
        // Your code BEGINS HERE

		SetColorProportion(40, 30, 30);

        // Your code ENDS HERE
    }

    // Sets the proportions of each colored counter in the generators during the game
    public void SetColorProportion(int redP, int yellowP, int blueP)
    {
        colorBag.Clear();
        int i;
        if (redP + yellowP + blueP == 100)
        {
            for (i = 0; i < redP; i++)
            {
                colorBag.Add(Constants.RED);
            }
            for (i = 0; i < yellowP; i++)
            {
                colorBag.Add(Constants.YELLOW);
            }
            for (i = 0; i < blueP; i++)
            {
                colorBag.Add(Constants.BLUE);
            }
        }
        else
        {
            Debug.LogError("Invalid Proportion!");
        }
    }

    private void SetDefaultColorProportion()
    {
        if (colorBag.Count > 0) return;
        colorBag = new List<int> { Constants.RED, Constants.YELLOW, Constants.BLUE };
    }

    override public string ToString()
    {
        string returnString = "shuttles: " + shuttleNum + "\n" +
            "gridSize: " + gridSize + "\n" +
            "minAnchorDis: " + minAnchorDis + "\n" +
            "counterNumInGenerator: " + counterNumInGenerator + "\n" +
            "carryLimit: " + carryLimit + "\n" +
            "anchorCount: " + anchorCount + "\n" +
            "randomAnchor: " + randomAnchor + "\n" +
            "timeLimit: " + timeLimit + "\n" +
            "tokenColors: " + tokenColors + " \n" +
            "blocksPerTurn: " + blocksPerTurn + " \n" +
            "mode: " + mode + "\n" +
            "selectedAgent: " + selectedAgent + " \n" +
            "selectedBlocker: " + selectedBlocker + "\n" +
            "searchDirections: " + searchDirections + "\n";


        return returnString;

            //public bool diags;

            //public Vector3[] regionAnchors = new Vector3[2];//to pass from agent to AIBehavior

            //public List<Vector3> defaultAnchorPos;
    }
}
