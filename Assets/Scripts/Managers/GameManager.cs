/*
 * The GameManager controls logic of the game, singleton
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // Current turn is player's turn or AI's turn
    [HideInInspector] public bool playerTurn;
    // Game over signal
    [HideInInspector] public bool gameOver;
    // AI gonna rotation when it's true
    [HideInInspector] public bool AICelebrate;

    // Game sprites
    public GameObject gridTile;
    public GameObject Anchor;
    public GameObject OnAnchor;
    public GameObject[] GeneratorsImages;
    public GameObject[] pickupTiles;
    public GameObject[] counterTiles;
    public GameObject[] counterOnShuttleTiles;
    public GameObject AI;

    // Stores what happened in the game
    [HideInInspector] public string gameLog;

    // Red, White, Blue, Yellow Generators
    [HideInInspector] public List<GameObject> generators = new List<GameObject>();
    // Parking position of each generator above - set by BoardGenerator
    [HideInInspector] public List<Vector3> parkingPos = new List<Vector3>();
    // The center position of each anchor
    [HideInInspector] public List<Vector3> anchorPositions = new List<Vector3>();
    // The colors of deposited counters, deposited[x][y] == -1 means empty grid, >10 indicates stack (topmost color = value-10).
    [HideInInspector] public List<List<int>> deposited = new List<List<int>>();
    //if multiple deposits (one on top of other) all appear in list. Lasty = topmost, reproduced in deposited.
    public Dictionary<Vector2,List<int>> depositStack = new Dictionary<Vector2, List<int>>();
    // blocked[x][y] == true: (x, y) has been blocked
    [HideInInspector] public List<List<bool>> blocked = new List<List<bool>>();
    // Use to prevent counter turned over immediately after deposit
    [HideInInspector] public List<List<bool>> readyToTurnOver = new List<List<bool>>();
    // Store GameObjects of counters deposited on the board
    [HideInInspector] public List<List<GameObject>> countersOnBoard = new List<List<GameObject>>();
    // Convincers displayed, color in z param.
    [HideInInspector] public List<Vector3> convincers = new List<Vector3>();

    // Board Generator, creates the board and generators
    private BoardGenerator boardScript;
    // AI Manager, controls AI moves
    private AIManager aiScript;
    private UIManager UI;
    private TileController TC;
    IPlayerB blocker;
    IPlayerA customScript;
    private int[] savedGameParams = new int[20];
    private List<Vector3> playerBlocks = new List<Vector3>();
    private List<Actions> AIactions;

    private bool preGame = false;
    private bool human = true;

    private List<Vector3> trueAnchors;
    private int realTotalSteps;
    private int AIscore;
    private int playerScore;

    private GameLog newGameLog = new GameLog();

    private void Awake()
    {
		Debug.Log("Initialising Game Manager.");
        Debug.Log("Logfile saved to: " + Application.persistentDataPath);
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }


    // Initialize the whole game (called from StartGame())
    private void Initialize()
    {
        string tempString;
        gameLog = "";
        gameOver = false;
        AICelebrate = false;
        playerTurn = false;
        boardScript = GetComponent<BoardGenerator>();
        boardScript.SetupScene();

        customScript = GeneratePlayer();
        if (!GameParameters.instance.selectedBlocker.Equals("Human"))
        {
            human = false;
            blocker = GenerateBlocker();
        }
         
        aiScript = GetComponent<AIManager>();
        aiScript.InitialiseAIs(customScript);
        InitialiseDeposited();
		UI = GetComponent<UIManager>();
        TC = GetComponent<TileController>();
        if (GameParameters.instance.mode == Constants.ONLINE)
        {
            preGame = true;
        }

        trueAnchors = customScript.RegisterTrueAnchors();
        //Get tokens required to join for real anchors
        realTotalSteps = PathFinder.GetStepsRemaining(trueAnchors[0],trueAnchors[1]);

        //for (int i = 0; i < trueAnchors.Count; i++)
        //    Debug.Log(i + ": (" + trueAnchors[i].x + "," + trueAnchors[i].y + ")");
        //Debug.Log("(5,5) far from? = " + Methods.FarFrom(new Vector3(5, 5, 0), trueAnchors));


        //log settings
        newGameLog.Clear();
        newGameLog.Log(Config.gameParamString);
        tempString = "Anchor Positions: "; 
        foreach(Vector3 pos in anchorPositions)
        {
            tempString += "(" + pos.x + "," + pos.y + ")";
        }
        newGameLog.Log(tempString);
        newGameLog.Log("\n--- Game Start ---");
    }


    private IPlayerA GeneratePlayer()
    {
        string scriptName = GameParameters.instance.selectedAgent;//from drop down or params
        Type player = Type.GetType(scriptName);//convert to type
        return (IPlayerA)Activator.CreateInstance(player);
    }

    private IPlayerB GenerateBlocker()
    {
        string scriptName = GameParameters.instance.selectedBlocker;//from drop down or params
        Type player = Type.GetType(scriptName);//convert to type
        return (IPlayerB)Activator.CreateInstance(player);
    }


    // Start AI's turn 
    public IEnumerator TurnSwitch()
    {
		if (!gameOver)
        {
            playerTurn = false;
            if (human) yield return new WaitForSeconds(1);
            if (GameParameters.instance.mode == Constants.INTERRUPTED)//wait for keypress before showing AI turn
            {
                UI.WriteToScreen("Press <SPACEBAR> to continue.");
                while (!Input.GetKeyDown(KeyCode.Space))
                {
                    yield return null;
                }
                UI.WriteToScreen("");
            }
            else if (preGame)//only occurs when mode = ONLINE
            {
                yield return StartCoroutine(UI.WaitForPlayer());
                preGame = false;
            }

            //start AI's turn - display AI turn panel
            yield return StartCoroutine(UI.ShowAITurn());
            //int score = EvaluateBlocks();
            EvaluateBlocks(); //maintains displayed score

            //calculate path completion as score passed to player A
            int currentStepsRemaining = PathFinder.GetStepsRemaining(trueAnchors[0], trueAnchors[1]);
            int score =  0;
            if (realTotalSteps - currentStepsRemaining > 0)
            {
                score = (int)((double)(realTotalSteps - currentStepsRemaining) / realTotalSteps * 100);
            }
            Debug.Log("original shortest pathlength = " + realTotalSteps + ", steps remaining = " + currentStepsRemaining);
            Debug.Log("current score (path completion as percentage based on steps remaining) = " + score);

            AIactions = aiScript.AITurn(playerBlocks, score, UI.timeRemaining);

            if(AIactions[0].commands.Count + AIactions[1].commands.Count == 2)//just "finished"
            {
                GameOverPlayerWin();
            }
        }
    }

    //calculates score based on playerBlocks
    private int EvaluateBlocks()
    {
        if (playerBlocks.Count == 0) return 0;
        int eval = 0;
        int tally = 0;
        foreach (Vector3 block in playerBlocks)
        {
            eval = Methods.FarFrom(block, trueAnchors);
            playerScore += Mathf.Max(10 - eval, 0);
            if (eval == 0)
            {
                AIscore -= 10;
            }
            tally += eval/GameParameters.instance.gridSize;
        }

        AIscore += tally;
        return eval;
    }

    //executes turn when player B is not human
    public IEnumerator PlayerTurn()
    {
        if (!gameOver)
        {
            playerBlocks = blocker.DecideBlocks(AIactions, convincers, UI.timeRemaining);
            convincers.Clear();// don't send same convincers twice!
            List<GameObject> selectedTiles = new List<GameObject>();

            GameObject[] gridtiles = GameObject.FindGameObjectsWithTag("Floor");
            //int limit = Math.Min(blocks.Count, GameParameters.instance.blocksPerTurn);


            yield return new WaitForSeconds(1);
            //for (int i = 0; i < limit; i++)
            
            foreach (Vector3 pos in playerBlocks)
            { 
                foreach (GameObject tile in gridtiles)
                {
                    if (tile.transform.position == pos)
                    {
                        selectedTiles.Add(tile);
                    }
                }
                
            }
            foreach(GameObject tile in selectedTiles)
            {
                tile.GetComponent<TileController>().ExecuteBlock(tile.transform.position);
                yield return new WaitForSeconds(1);
            }
        }
    }


    // Check if there is a red path between two anchors
    public bool CheckGameOver()
    {
        foreach (Vector3 position in anchorPositions)
        {
            if (Methods.BFStoAnotherAnchor(new Vector3(position.x - 0.5f, position.y - 0.5f, 0f)))
            {
                WaitSecs(2f);
                return true;
            }
        }
        return false;
    }

    private IEnumerator WaitSecs(float secs)
    {
        yield return new WaitForSeconds(secs);
    }

    // Show AI Win (path-completed) and write the result to gameLog
    public void GameOverAIWin()
    {
        newGameLog.Log( "--- Path completed ---");
        newGameLog.Log( "Remaining Time For AI: " + UI.timeRemaining + " seconds");
        gameOver = true;
        AICelebrate = true;
        UI.ShowAIWinText(AIscore, playerScore);
        Methods.TurnAllWhiteCounterOver();
        aiScript.TurnOffAIs();
        SendToServer();
    }

    // Show Player Win (timeout) and write the result to gameLog
    public void GameOverPlayerWin()
    {
        //Debug.Log("trying to write that AI timed out.");
        //UI.WriteToScreen("Logfile saved to: " + Application.persistentDataPath);
        newGameLog.Log("AI timed out.");
        newGameLog.Log( "--- Player Wins ---");
        gameOver = true;
        UI.ShowPlayerWinText(AIscore, playerScore);
        Methods.TurnAllWhiteCounterOver();
        aiScript.TurnOffAIs();
        SendToServer();
    }

    // Initializes lists which use to record the game state
    private void InitialiseDeposited()
    {
        deposited.Clear();
        readyToTurnOver.Clear();
        blocked.Clear();
        countersOnBoard.Clear();
        depositStack.Clear();
        playerBlocks.Clear();
        for (int x = 0; x < GameParameters.instance.gridSize; x++)
        {
            deposited.Add(new List<int>());
            readyToTurnOver.Add(new List<bool>());
            blocked.Add(new List<bool>());
            countersOnBoard.Add(new List<GameObject>());
            for (int y = 0; y < GameParameters.instance.gridSize; y++)
            {
                deposited[x].Add(-1);
                readyToTurnOver[x].Add(false);
                blocked[x].Add(false);
                countersOnBoard[x].Add(null);
            }
        }
    }


    public void LogBlocks(int blockCount, Vector3 pos)
    {
        if (blockCount == 1) { playerBlocks.Clear(); }//restart
        playerBlocks.Add(pos);
        instance.blocked[(int)pos.x][(int)pos.y] = true;
        newGameLog.Log( "PlayerB blocks " + pos);
    }

    public Vector3 GetParkingPosition(int generatorId)
    {
        return parkingPos[generatorId];
    }


    public List<Vector3> GetLatestBlocks()
    {
        return playerBlocks;
    }


    private void DestroyObjects(GameObject[] objects)
    {
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }

    public void ResetGame()
    {
        aiScript.StopAllCoroutines();
        UI.runTimer = false;
        StartGame();
    }

    // Clear objects and initialize a new game
    public void StartGame()
    {
        foreach (GameObject obj in generators)
        {
            Destroy(obj);
        }
        GameObject[] objects;
        objects = GameObject.FindGameObjectsWithTag("PickUp");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("Counter");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("WhiteCounter");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("Shuttle");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("OnShuttle");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("WhiteOnShuttle");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("Floor");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("Anchor");
        DestroyObjects(objects);
        objects = GameObject.FindGameObjectsWithTag("Board");
        DestroyObjects(objects);
        Methods.InitializeMethods();
        Initialize();
        UI.Initialize();
        //aiScript.InitialiseAIs(customScript);


        StartCoroutine(TurnSwitch());
    }

    // Sends the gameLog to Server after gameover
    private void SendToServer()
    {
        DataLog.Save(newGameLog);
    }


    // Called when the scene loaded
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            StartGame();
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void Update()
    {
        if (!human && playerTurn)
        {
            //Debug.Log("not human and playerturn");
            playerTurn = false;
            StartCoroutine(PlayerTurn());
        }
    }
}
