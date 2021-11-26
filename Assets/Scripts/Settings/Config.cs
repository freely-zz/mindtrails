
public static class Config
{
    //Sets defaults and stores parameters between scenes

    public static int gridSize = 40;
    public static int anchorCount = 10;
    public static float minAnchorDis = 12;
    public static int shuttleNum = 2;
    public static int carryLimit = 4;
    public static int moveLimit = 4;

    public static float timeLimit = 180;
	public static int blocksPerTurn = 3;

    public static int tokenColors = 3;
    public static bool diags = true;
    public static bool convincers = true;
    public static bool faceDown = true;
    
    public static string selectedAgent = "A_PlayableDemo";//must be existing IPlayerA class
    public static string selectedBlocker = "Human";//must be existing IPlayerB class or "Human"
    public static int mode = Constants.IMMEDIATE;

    public static string gameParamString;
}

