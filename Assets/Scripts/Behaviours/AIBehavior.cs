/*
 * AIBehavior includes AI's properties such as information about carrying counters. 
 * Attached to shuttles.
 */

using System.Linq;
using UnityEngine;

public class AIBehavior : MonoBehaviour
{
    //public static AIBehavior instance;

    public int[] carry = new int[3];//counters by color
    // Turns over the counters how many seconds when the shuttle collides with them
    public float turnOverDelay = 0.5f;
    public int[] bagCounterColor = { -1, -1, -1, -1 };//counters by index
    public GameObject[] counterInBag = new GameObject[4];//"physical" counters
    private float speed = 3f;


    private void OnTriggerStay2D(Collider2D collision)
    {
		if (GameParameters.instance.convincers)
		{
	        if (carry.Sum() == 0 && collision.gameObject.CompareTag("WhiteCounter"))
	        {
	            Vector3 pos = collision.gameObject.transform.position;

				if (Methods.ReadyToTurnOver(pos))
	            {
	                Methods.SetReadyToTurnOver(pos, false);
	                StartCoroutine(Methods.TurnWhiteCounterOver(pos, turnOverDelay, this.gameObject));
	            }
	        }
		}
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // The shuttle may exit the counter before it turns back to white
        if (collision.gameObject.CompareTag("WhiteCounter") || collision.gameObject.CompareTag("Counter"))
        {
            Vector3 pos = collision.gameObject.transform.position;
            Methods.SetReadyToTurnOver(pos, true);
        }
    }

    // Returns the number of each counter
    public int[] GetCarryColor()
    {
        int[] copyCarry = new int[3];
        GetComponent<AIBehavior>().carry.CopyTo(copyCarry, 0);
        return copyCarry;
    }

    // Returns the color of each counter in the shuttle's bag
    public int[] GetBagCounterColor()
    {
        int[] copyBag = new int[4];
        GetComponent<AIBehavior>().bagCounterColor.CopyTo(copyBag, 0);
        return copyBag;
    }

    public void SetSpeed(float s)
    {
        speed = s;
    }

    public float GetSpeed()
    {
        return speed;
    }
}
