/*
 * The UIManager holds the timer, displays tips when switching turns and the end of the game.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public float showTurnDelay = 5f;
    public float delayBeforeAITurn = 0.3f;
    public float timeRemaining;

    public Text timer;
    public Text waitingText;
    public Text playerBlocksText;
    public Text playerAText;
    public Text playerBText;
    public Text playerAScore;
    public Text playerBScore;
    public GameObject AITurnPanel;
    public GameObject PlayerTurnPanel;
    public GameObject AIWinPanel;
    public GameObject PlayerWinPanel;
    public GameObject RestartButton;
    public GameObject ScorePanelA;
    public GameObject ScorePanelB;
    public GameObject EndScreen;

    public bool runTimer = false;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        Debug.Log("Initialising UI Manager.");
        AITurnPanel.SetActive(false);
        PlayerTurnPanel.SetActive(false);
        AIWinPanel.SetActive(false);
        PlayerWinPanel.SetActive(false);
        RestartButton.SetActive(false);
        ScorePanelA.SetActive(false);
        ScorePanelB.SetActive(false);
        EndScreen.SetActive(false);
        playerAScore.fontStyle = FontStyle.Bold;
        playerBScore.fontStyle = FontStyle.Bold;

        waitingText.text = "";
        timeRemaining = GameParameters.instance.timeLimit;
        timer.text = " " + Mathf.Floor(timeRemaining / 60).ToString("00") + ":" + Mathf.Floor(timeRemaining % 60).ToString("00");

        if (GameParameters.instance.blocksPerTurn > 1)
        {
            playerBlocksText.text = "You have " + GameParameters.instance.blocksPerTurn + " blocks!";
        }
        else
        {
            playerBlocksText.text = "";
        }

    }

    //path completed
    public void ShowAIWinText(int AIscore, int playerScore)
    {
        AIWinPanel.SetActive(true);
        RestartButton.SetActive(true);
        playerAScore.text = AIscore.ToString("000");
        playerBScore.text = playerScore.ToString("000");
        HighlightScores(AIscore, playerScore);
        ScorePanelA.SetActive(true);
        ScorePanelB.SetActive(true);
        waitingText.text = "";
        EndScreen.SetActive(true);
    }

    //timeout
    public void ShowPlayerWinText(int AIscore, int playerScore)
    {
        PlayerWinPanel.SetActive(true);
        RestartButton.SetActive(true);
        playerAScore.text = AIscore.ToString("000");
        playerBScore.text = playerScore.ToString("000");
        HighlightScores(AIscore, playerScore);
        ScorePanelA.SetActive(true);
        ScorePanelB.SetActive(true);
        waitingText.text = "";
        EndScreen.SetActive(true);
    }

    private void HighlightScores(int AIscore, int playerScore)
    {
        if (AIscore > playerScore)
        {
            playerAText.fontStyle = FontStyle.Bold;
            playerAScore.fontStyle = FontStyle.Bold;
            playerBText.fontStyle = FontStyle.Normal;
            playerBScore.fontStyle = FontStyle.Normal;
        }
        else if (playerScore > AIscore)
        {
            playerAText.fontStyle = FontStyle.Normal;
            playerAScore.fontStyle = FontStyle.Normal;
            playerBText.fontStyle = FontStyle.Bold;
            playerBScore.fontStyle = FontStyle.Bold;
        }
    }
    public IEnumerator ShowPlayerTurn()
    {
        if (GameParameters.instance.mode == Constants.INTERRUPTED)
        {
            runTimer = false;
        }
        PlayerTurnPanel.SetActive(true);
        yield return new WaitForSeconds(showTurnDelay);
        PlayerTurnPanel.SetActive(false);
    }


    public IEnumerator ShowAITurn()
    {
        yield return new WaitForSeconds(delayBeforeAITurn);
        AITurnPanel.SetActive(true);
        yield return new WaitForSeconds(showTurnDelay);
        AITurnPanel.SetActive(false);
        runTimer = true;
    }


    void FixedUpdate()
    {
        if (runTimer && !GameManager.instance.gameOver)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining > 0)
            {
                float minutes = Mathf.Floor(timeRemaining / 60);
                float seconds = Mathf.Floor(timeRemaining % 60);

                timer.text = " " + minutes.ToString("00") + ":" + seconds.ToString("00");
            }
            else
            {
                timer.text = " 00:00";
                GameManager.instance.GameOverPlayerWin();
            }
        }
    }

    public void WriteToScreen(string message)
    {
        waitingText.color = Color.red;
        waitingText.text = message;
    }

    public IEnumerator WaitForPlayer()
    {
        List<string> names = new List<string>() { "andyp", "batman", "cincs", "dhirendra" };
        List<string> messages = new List<string>(new string[] { "Waiting for other player to join .    ", "Waiting for other player to join . .  ", "Waiting for other player to join . . ." });
        int index;
        int waitTime = Random.Range(5, 20);
        waitingText.color = Color.red;
        for (int i = 0; i < waitTime; i++)
        {
            index = i % messages.Count;
            waitingText.text = messages[index];
            yield return new WaitForSeconds(1);
        }
        waitingText.color = Color.gray;
        waitingText.alignment = TextAnchor.MiddleRight;
        waitingText.text = names[Random.Range(0, 4)];
    }
}
