using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearColorFruit : ClearedFruit
{
    private ColorFrult.ColorType clearColor;
    public ColorFrult.ColorType ClearColor { get => clearColor; set => clearColor = value; }

    public override void Clear()
    {
        base.Clear();
        fruit.gameManager.ClearColoer(clearColor);
    }
}
