using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFruit : MonoBehaviour
{
    // Òþ²Ø¼àÊÓÃæ°å
    [HideInInspector]
    public GameManager gameManager;
    private int x;
    public int X
    {
        get
        {
            return x;
        }
        set
        {
            if (canMove())
            {
                x = value;
            }
        }
    }
    private int y;
    public int Y
    {
        get
        {
            return y;
        }
        set
        {
            if (canMove())
            {
                y = value;
            }
        }
    }
    public GameManager.FruitType Type
    {
        get
        {
            return type;
        }
    }
    private GameManager.FruitType type;
    private MovedFruit movedComponent;
    public MovedFruit MovedComponent
    {
        get
        {
            return movedComponent;
        }
    }

    private ColorFrult coloredComponent;
    public ColorFrult ColoredComponent
    {
        get
        {
            return coloredComponent;
        }
    }

    private ClearedFruit clearComponent;
    public ClearedFruit ClearComponent { get => clearComponent; }

    

    public bool canMove()
    {
        return movedComponent != null;
    }

    public bool canColor()
    {
        return coloredComponent != null;
    }

    public bool canClear()
    {
        return clearComponent != null;
    }


    private void Awake()
    {
        movedComponent = GetComponent<MovedFruit>();
        coloredComponent = GetComponent<ColorFrult>();
        clearComponent = GetComponent<ClearedFruit>();
    }



    public void Init(int _x, int _y, GameManager _gameManager, GameManager.FruitType _type)
    {
        x = _x;
        y = _y;
        gameManager = _gameManager;
        type = _type;
    }

    private void OnMouseEnter()
    {
        gameManager.EnterFruit(this);
    }

    private void OnMouseDown()
    {
        gameManager.PressFruit(this);
    }

    private void OnMouseUp()
    {
        gameManager.ReleaseFruit();
    }

}
