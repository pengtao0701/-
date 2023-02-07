using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorFrult : MonoBehaviour
{

    public enum ColorType
    {
        APPLE,
        BANANA,
        ORANGE,
        LEMON,
        COCONUT, // вЌзг
        STRAWBERRY,
        PEAR,
        ANY,
        COUNT
    }

    [System.Serializable]
    public struct ColorSprite
    {
        public ColorType color;
        public Sprite sprite;
    }

    private ColorType color;
    public ColorType Color 
    {
        get 
        {
            return color;
        }
        set 
        { 
            SetColor(value); 
        } 
    }

    public ColorSprite[] colorsSprites;

    private Dictionary<ColorType, Sprite> colorSpriteMap;

    private SpriteRenderer spriteRenderer;

    public int NumColors
    {
        get { return colorsSprites.Length; }
    }

    

    private void Awake()
    {
        spriteRenderer = transform.Find("Fruit").GetComponent<SpriteRenderer>();
        if (colorSpriteMap == null)
        {
            colorSpriteMap = new Dictionary<ColorType, Sprite>();
        }

        for (int i =0; i< colorsSprites.Length; i++)
        {
            if (!colorSpriteMap.ContainsKey(colorsSprites[i].color))
            {
                colorSpriteMap.Add(colorsSprites[i].color, colorsSprites[i].sprite);
            }
        }
        //foreach (var kv in colorSpriteMap)
        //{
        //    Debug.Log("ColorType= " + kv.Key + "," + "Sprite= " + kv.Value);
        //}
        
    }

    public void SetColor(ColorType newColor)
    {
        color = newColor;
        if (colorSpriteMap.ContainsKey(newColor))
        {
            spriteRenderer.sprite = colorSpriteMap[newColor];
        }
    }
}
