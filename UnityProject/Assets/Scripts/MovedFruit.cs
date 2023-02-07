using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovedFruit : MonoBehaviour
{
    private GameFruit fruit;

    // 单独移动时使用携程
    private IEnumerator moveCoroutine;
    private void Awake()
    {
        fruit = GetComponent<GameFruit>();
    }

    // 移动
    public void Move(int newX, int newY, float time)
    {
        //fruit.X = newX;
        //fruit.Y = newY;
        //fruit.transform.position = fruit.gameManager.FixGridPosition(newX, newY);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = MoveCoroutine(newX, newY, time);
        StartCoroutine(moveCoroutine);
    }

    // 负责移动的携程
    private IEnumerator MoveCoroutine(int newX, int newY,float time)
    {
        fruit.X = newX;
        fruit.Y = newY;

        // 每一帧移动一点
        Vector3 startPos = transform.position;

        Vector3 endPos = fruit.gameManager.FixGridPosition(newX, newY);

        for (float t = 0; t < time; t+= Time.deltaTime)
        {
            // 平滑差帧Vector3.Lerp
            fruit.transform.position = Vector3.Lerp(startPos, endPos,t/time);
            yield return 0;
        }

        // 时间结束时还未到指定位置 则强制位移
        fruit.transform.position = endPos;
    }
}
