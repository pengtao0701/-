using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearLineFruit : ClearedFruit
{

    public bool isRow;

    public override void Clear()
    {
        base.Clear();
        if (isRow)
        {
            fruit.gameManager.ClearRow(fruit.Y);
        }
        else
        {
            fruit.gameManager.ClearColumn(fruit.X);
        }
    }
}
