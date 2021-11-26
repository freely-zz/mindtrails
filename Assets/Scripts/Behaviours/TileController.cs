/*
 * The TileController monitors human blocking on human’s turn
 */

using UnityEngine;
using System.Collections.Generic;

public class TileController : MonoBehaviour
{
	private static int blockCounter = 1;//tally of blocks per turn

    private void OnMouseDown()
    {
         if (!GameParameters.instance.selectedBlocker.Equals("Human"))
        {
            return;
        }
        else if (!GameManager.instance.gameOver && GameManager.instance.playerTurn)
        {
            ExecuteBlock(transform.position);
        }

    }

    public void ExecuteBlock(Vector3 pos)
    {
        if (Methods.IsEmptyGrid(pos))//only if grid location is empty
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.color = new Color(0f, 0f, 0f, 0.8f);
            transform.gameObject.tag = "Blocked";
            GameManager.instance.LogBlocks(blockCounter, transform.position);
            GameManager.instance.gameLog += "Player blocks " + transform.position + "\n";
            blockCounter++;
        }
        if (blockCounter > GameParameters.instance.blocksPerTurn)
        {
            blockCounter = 1;
            StartCoroutine(GameManager.instance.TurnSwitch());
        }  
    }

}
