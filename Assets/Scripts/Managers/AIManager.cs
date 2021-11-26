/*
 * The AIManager is responsible to control the movements of shuttles. It gets decisions from the AIAgent and executes these actions on AI’s turn.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AIManager : MonoBehaviour
{
    //public int turnCount;

    private float moveDelay = 0f;
    private float collectDelay = 0.3f;
    private float defaultDepositDelay = 0.1f;

    private List<GameObject> AIs = new List<GameObject>();//shuttles
    private List<Vector3> bagPos = new List<Vector3>();//generators
    private List<bool> AIMoving = new List<bool>();
    private Vector3 proposedPark;

    private IPlayerA customScript; //reference to player created by GameManager

	public List<GameObject> getAIs(){
		return AIs;
	}

    private void Awake()
    {
		Debug.Log("Initialising AI Manager.");
        bagPos.Clear();
        bagPos.Add(new Vector3(-1.6f, 0.02f, 0f));
        bagPos.Add(new Vector3(-0.5f, 0.02f, 0f));
        bagPos.Add(new Vector3(0.55f, 0.02f, 0f));
        bagPos.Add(new Vector3(1.65f, 0.02f, 0f));
    }



    //called from GameManager
    public void InitialiseAIs(IPlayerA customScript)
    {
        this.customScript = customScript;
        AIs.Clear();
        AIMoving.Clear();
        proposedPark = Vector3.zero;
        for (int i = 0; i < GameParameters.instance.shuttleNum; i++)
        {
            AIs.Add(Methods.LayoutObject(GameManager.instance.AI, 0f, 0f));
            AIs[i].transform.position = new Vector3(-3.5f, (GameParameters.instance.gridSize / 2f) + i * 1.5f, 0f);
            AIMoving.Add(true);
        }

    }

    public void TurnOffAIs()
    {
        for (int i = 0; i < AIs.Count; i++)
        {
            AIs[i].SetActive(false);
        }
    }

    public List<Actions> AITurn(List<Vector3> blocks, int thisScore, float timeRemaining)
    {
        Debug.Log("AI Turn");
        List<Actions> AIactions;
  
        AIactions = customScript.MakeDecision(blocks, thisScore, timeRemaining);
        for (int i = 0; i < AIs.Count; i++)
        {
            //Debug.Log("Shuttle " + i + " Decisions: -------------------");
            AIactions[i].commands.Add("Finished");
            AIMoving[i] = true;
            StartCoroutine(ExecuteActions(AIactions[i],i));
        }

        return AIactions;
    }

    IEnumerator ExecuteActions(Actions actions, int AIindex)
    {    
        for (int i = 0; i < actions.commands.Count; i++)
        {
            if (GameManager.instance.gameOver) break;
            string[] commands = actions.commands[i].Split('#');
            switch (commands[0])
            {
                case "Park":
                    Vector3 tempPos = AIs[AIindex].transform.position;
                    float posx = tempPos.x;
                    float posy = tempPos.y;
                    if (posx < (GameParameters.instance.gridSize / 2)) {
                        posx = -3.5f;
                    } else {
                        //parking position will be off right so move up first
                        yield return StartCoroutine(MoveToPosition(0, AIs[AIindex], new Vector3(tempPos.x+ 1, tempPos.y + 1, 0)));
                        posx = GameParameters.instance.gridSize + 2.5f;
                    }
                    posy = GetNonParkY(AIs[AIindex].transform.position);
                    yield return StartCoroutine(MoveToPosition(0, AIs[AIindex], new Vector3(posx, posy, 0)));
                    break;
                case "Finished":
                    AIMoving[AIindex] = false;
                    break;
                case "ShuttleNum":
                    AIindex = Int32.Parse(commands[1]);
                    break;
                case "Wait":
					yield return new WaitForSeconds(actions.paras[i].z);
					break;
				case "Speed":
                    AIs[AIindex].GetComponent<AIBehavior>().SetSpeed(actions.paras[i].z);
					break;
                case "Collect":
                    if (AIs[AIindex].GetComponent<AIBehavior>().carry.Sum() < GameParameters.instance.carryLimit && Methods.OnParkingPos(AIs[AIindex].transform.position))
                    {
                        int generatorId = Methods.FindGenerator(AIs[AIindex].transform.position);
                        GameManager.instance.gameLog += "Shuttle " + AIindex + " collects from Generator " + generatorId + " ";
                        yield return StartCoroutine(CollectCounter(AIs[AIindex], generatorId, actions.paras[i]));
                    }
                    break;
                case "Move":
                    yield return StartCoroutine(MoveToPosition(moveDelay, AIs[AIindex], actions.paras[i]));
                    GameManager.instance.gameLog += "Shuttle " + AIindex + " moves to " + "(" + actions.paras[i].x + ", " + actions.paras[i].y + ")" + "\n";
                    break;
                case "Deposit":
                    Vector3 pos = new Vector3(actions.paras[i].x, actions.paras[i].y, 0f);
                    int num = Int32.Parse(commands[2]);
                    if (commands[1].Equals("Color"))
                    {
                        if (AIs[AIindex].transform.position == pos)
                        {
                            yield return StartCoroutine(DepositCounter(AIs[AIindex], pos, num, actions.paras[i].z));
                            GameManager.instance.gameLog += "Shuttle " + AIindex + " deposits at " + "(" + pos.x + ", " + pos.y + ")" + ", color: " + num + "\n";

                        }
                    }
                    else
                    {
                        if (AIs[AIindex].transform.position == pos)
                        {
                            yield return StartCoroutine(DepositCounterByIndex(AIs[AIindex], pos, num, actions.paras[i].z));
                            GameManager.instance.gameLog += "Shuttle " + AIindex + " deposits at " + "(" + pos.x + ", " + pos.y + ")" + ", index: " + num + "\n";
                        }
                    }
                    break;
                case "TurnOver":
                    int index = Int32.Parse(commands[1]);
                    TurnOverCounterInBag(AIs[AIindex], index);
                    GameManager.instance.gameLog += "Shuttle " + AIindex + " turns over bag " + index + "\n";
                    break;
                case "CollectFromGrid":
                    yield return StartCoroutine(CollectCounterFromGrid(AIs[AIindex], actions.paras[i]));
                    GameManager.instance.gameLog += "Shuttle " + AIindex + " collects from " + "(" + actions.paras[i].x + ", " + actions.paras[i].y + ")" + "\n";
                    break;
                case "CollectFromBoard":
                    yield return StartCoroutine(CollectCounterFromGrid(AIs[AIindex], actions.paras[i]));
                    GameManager.instance.gameLog += "Shuttle " + AIindex + " collects from " + "(" + actions.paras[i].x + ", " + actions.paras[i].y + ")" + "\n";
                    break;
            }
            if (GameManager.instance.CheckGameOver())
            {
                GameManager.instance.GameOverAIWin();
            }
        }
        
    }

    private IEnumerator MoveToPosition(float delay, GameObject AI, Vector3 newPos)
    {
        float moveSpeed = AI.GetComponent<AIBehavior>().GetSpeed();
        while (AI.transform.position != newPos)
        {
            AI.transform.position = Vector3.MoveTowards(AI.transform.position, newPos,  moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator DepositCounter(GameObject AI, Vector3 pos, int color, float delay)
    {
        if (AI.GetComponent<AIBehavior>().carry[color] > 0 && Methods.IsEmptyGrid(pos))
        {
            yield return StartCoroutine(MoveToBagPosition(AI, GetBagPosByColor(AI, color)));
            if (Mathf.Approximately(delay, 0f))
            {
                yield return new WaitForSeconds(defaultDepositDelay);
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }
            GameManager.instance.countersOnBoard[(int)pos.x][(int)pos.y] = Methods.LayoutObject(GameManager.instance.counterTiles[Constants.WHITE], pos.x, pos.y);
            AI.GetComponent<AIBehavior>().carry[color]--;
            GameManager.instance.deposited[(int)pos.x][(int)pos.y] = color;
            DelFromBag(AI, color);
        }
    }

    private IEnumerator DepositCounterByIndex(GameObject AI, Vector3 pos, int index, float delay)
    {
        int counterColor = AI.GetComponent<AIBehavior>().bagCounterColor[index];
        if (counterColor != -1)//if there's something in the bag at this pos
        {
            yield return StartCoroutine(MoveToBagPosition(AI, index));
            if (Mathf.Approximately(delay, 0f))
            {
                yield return new WaitForSeconds(defaultDepositDelay);
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }
            if (Methods.IsEmptyGrid(pos))//nothing on grid at this position
            {
                //populate with reference to actual (white) game object
                GameManager.instance.countersOnBoard[(int)pos.x][(int)pos.y] = Methods.LayoutObject(GameManager.instance.counterTiles[Constants.WHITE], pos.x, pos.y);
            }
            else//something already there
            {
                Vector2 location = new Vector2(pos.x, pos.y);
                //initialise stack
                if (!GameManager.instance.depositStack.ContainsKey(location))
                {
                    List<int> colors = new List<int>();
                    colors.Add(GameManager.instance.deposited[(int)pos.x][(int)pos.y]);//original color at 0
                    GameManager.instance.depositStack.Add(location, colors);
                }
                //add to existing stack
                GameManager.instance.depositStack[location].Add(counterColor);
                
            }
            //deposit records whatever's on top.
            GameManager.instance.deposited[(int)pos.x][(int)pos.y] = counterColor;
            AI.GetComponent<AIBehavior>().carry[AI.GetComponent<AIBehavior>().bagCounterColor[index]]--;
            DelFromBagByIndex(AI, index);
        }
    }


    IEnumerator CollectCounter(GameObject AI, int generatorId, Vector3 pos)
    {
        if (Methods.IsInGn(pos, generatorId) && GetEmptyBagPosIndex(AI) != -1)
        {
            GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().visitThisTurn = true;
            AI.GetComponent<AIBehavior>().carry[GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().GetPickupsInGnColor(pos)]++;
            GameManager.instance.gameLog += "color: " + GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().GetPickupsInGnColor(pos) + "\n";
            AddToBag(AI, GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().GetPickupsInGnColor(pos));
            GameManager.instance.generators[generatorId].GetComponent<GeneratorManager>().AddToRegenerateList(pos);
            yield return new WaitForSeconds(collectDelay);
        }
    }

    private IEnumerator CollectCounterFromGrid(GameObject AI, Vector3 pos)
    {
        int color = Methods.OnCounter(pos);
        int bagPosIndex = GetEmptyBagPosIndex(AI);
        yield return StartCoroutine(MoveToBagPosition(AI, bagPosIndex));
        if (color != -1 && bagPosIndex != -1)//if there's a color and there's room
        {
            //AI.GetComponent<BoxCollider2D>().enabled = false;//disable so counter doesn't redisplay (as if turned back over) while being removed!
            AI.GetComponent<AIBehavior>().carry[color]++;

            Vector2 location = new Vector2(pos.x, pos.y);

            if (GameManager.instance.depositStack.ContainsKey(location))//stack exists
            {
                //remove last from stack
                GameManager.instance.depositStack[location].RemoveAt(GameManager.instance.depositStack[location].Count - 1);
                //set GM desposited list to new topmost (now topmost removed)
               
                GameManager.instance.deposited[(int)pos.x][(int)pos.y] = GameManager.instance.depositStack[location][GameManager.instance.depositStack[location].Count - 1];
         
                if (GameManager.instance.depositStack[location].Count==1)//back to normal - just one counter on deck so don't need stack
                {
                    GameManager.instance.depositStack.Remove(location);
                }
                
            }
            else
            {

                // Hide the turned over color counter
                GameObject[] counters;
                counters = GameObject.FindGameObjectsWithTag("Counter");
                foreach (GameObject counter in counters)
                {
                    if (counter.transform.position == pos)
                    {
                        counter.SetActive(false);
                    }
                }
                
                GameManager.instance.deposited[(int)pos.x][(int)pos.y] = -1;
                GameManager.instance.countersOnBoard[(int)pos.x][(int)pos.y].SetActive(false);
                GameManager.instance.countersOnBoard[(int)pos.x][(int)pos.y] = null;
                Destroy(GameManager.instance.countersOnBoard[(int)pos.x][(int)pos.y]);
                
                // Do not turn back if game over or keep collision with shuttle
                //if (!GameManager.instance.gameOver && !colorcounter.GetComponent<BoxCollider2D>().IsTouching(AI.GetComponent<BoxCollider2D>()))
                

            }
            AddToBag(AI, color);
            yield return new WaitForSeconds(collectDelay);
            //AI.GetComponent<BoxCollider2D>().enabled = true;
        }
    }


    private int GetBagPosByColor(GameObject AI, int color)
    {
        for (int i = 0; i < 4; i++)
        {
            if (AI.GetComponent<AIBehavior>().bagCounterColor[i] == color)
            {
                return i;
            }
        }
        return -1;
    }

    private IEnumerator MoveToBagPosition(GameObject AI, int i)
    {
        float moveSpeed = AI.GetComponent<AIBehavior>().GetSpeed();

        Vector3 pos = bagPos[i];
        pos = new Vector3(AI.transform.position.x + (pos.x * (-1f)), AI.transform.position.y, 0f);
        while (AI.transform.position != pos)
        {
            AI.transform.position = Vector3.MoveTowards(AI.transform.position, pos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void DelFromBag(GameObject AI, int color)
    {
        for (int i = 0; i < 4; i++)
        {
            if (AI.GetComponent<AIBehavior>().bagCounterColor[i] == color)
            {
                AI.GetComponent<AIBehavior>().bagCounterColor[i] = -1;
                Destroy(AI.GetComponent<AIBehavior>().counterInBag[i]);
                break;
            }
        }
    }

    private void DelFromBagByIndex(GameObject AI, int index)
    {
        AI.GetComponent<AIBehavior>().bagCounterColor[index] = -1;
        Destroy(AI.GetComponent<AIBehavior>().counterInBag[index]);
    }

    private int GetEmptyBagPosIndex(GameObject AI)
    {
        for (int i = 0; i < 4; i++)
        {
            if (AI.GetComponent<AIBehavior>().bagCounterColor[i] == -1)
            {
                return i;
            }
        }
        return -1;
    }

    private void AddToBag(GameObject AI, int color)
    {
        int i = GetEmptyBagPosIndex(AI);
        AI.GetComponent<AIBehavior>().bagCounterColor[i] = color;
        AI.GetComponent<AIBehavior>().counterInBag[i] = Methods.LayoutObject(GameManager.instance.counterOnShuttleTiles[color], AI.transform.position.x + bagPos[i].x, AI.transform.position.y + bagPos[i].y);
        AI.GetComponent<AIBehavior>().counterInBag[i].transform.SetParent(AI.transform);
        if (GameParameters.instance.faceDown) MakeWhite(AI, i);
    }

    private void MakeWhite(GameObject AI, int i)
    //Routine added to make all pickups facedown on shuttle.
    {
        GameObject anotherCounter;
        if (AI.GetComponent<AIBehavior>().bagCounterColor[i] != -1)
        {
            anotherCounter = AI.GetComponent<AIBehavior>().counterInBag[i];
            if (AI.GetComponent<AIBehavior>().counterInBag[i].CompareTag("WhiteOnShuttle"))
            {
                //AI.GetComponent<AIBehavior>().counterInBag[i] = Methods.LayoutObject(GameManager.instance.counterOnShuttleTiles[AI.GetComponent<AIBehavior>().bagCounterColor[i]], AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.x, AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.y);
                //DO NOTHING
            }
            else
            {
                AI.GetComponent<AIBehavior>().counterInBag[i] = Methods.LayoutObject(GameManager.instance.counterOnShuttleTiles[3], AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.x, AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.y);
            }
            AI.GetComponent<AIBehavior>().counterInBag[i].transform.SetParent(AI.transform);
            Destroy(anotherCounter);
        }
    }

    private void TurnOverCounterInBag(GameObject AI, int i)
    {
        GameObject anotherCounter;
        if (AI.GetComponent<AIBehavior>().bagCounterColor[i] != -1)
        {
            anotherCounter = AI.GetComponent<AIBehavior>().counterInBag[i];
            if (AI.GetComponent<AIBehavior>().counterInBag[i].CompareTag("WhiteOnShuttle"))
            {
                AI.GetComponent<AIBehavior>().counterInBag[i] = Methods.LayoutObject(GameManager.instance.counterOnShuttleTiles[AI.GetComponent<AIBehavior>().bagCounterColor[i]], AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.x, AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.y);
            }
            else
            {
                AI.GetComponent<AIBehavior>().counterInBag[i] = Methods.LayoutObject(GameManager.instance.counterOnShuttleTiles[3], AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.x, AI.GetComponent<AIBehavior>().counterInBag[i].transform.position.y);
            }
            AI.GetComponent<AIBehavior>().counterInBag[i].transform.SetParent(AI.transform);
            Destroy(anotherCounter);
        }
    }

    // Checks if need to change y value.
    public float GetNonParkY(Vector3 pos)
    {
        float halfGrid = GameParameters.instance.gridSize / 2;
        float parkY;
        float yval = pos.y;

        for (int i = 0; i < GameManager.instance.generators.Count; i++)
        {
            parkY = GameManager.instance.parkingPos[i].y;
            if ( parkY <  halfGrid && yval < parkY)
            {
                yval = parkY + 2;
            }
            else if(parkY > halfGrid && yval > parkY)
            {
                yval = parkY - 2;
            }
        }
        //if other shuttle has stopped, what is its y? if same, change mine.
        if (proposedPark == Vector3.zero)
        {
            proposedPark = pos;
        }
        else
        {
            if (proposedPark == pos)
            {
                yval -= 1;
            }
            proposedPark = Vector3.zero;
        }
        return yval;
    }


    private void FixedUpdate()
    {
        //if both AIs have stopped (and not gameover) it's the player's turn
        for (int i = 0; i < GameParameters.instance.shuttleNum; i++)
        {
            if (AIMoving[i]) return;
        }
        if (!GameManager.instance.gameOver && !GameManager.instance.playerTurn)
        {
            AIMoving[0] = true;
            GameManager.instance.playerTurn = true;
            StartCoroutine(GetComponent<UIManager>().ShowPlayerTurn());

        }
    }
}
