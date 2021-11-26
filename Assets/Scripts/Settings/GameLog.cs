using System;

[Serializable]
public class GameLog
{
    public string gameLog = "";


    public void Log(string message)
    {
        gameLog += message + "\n";
    }

    public void Clear()
    {
        gameLog = "";
    }
}