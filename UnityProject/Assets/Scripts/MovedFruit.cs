using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovedFruit : MonoBehaviour
{
    private GameFruit fruit;

    // �����ƶ�ʱʹ��Я��
    private IEnumerator moveCoroutine;
    private void Awake()
    {
        fruit = GetComponent<GameFruit>();
    }

    // �ƶ�
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

    // �����ƶ���Я��
    private IEnumerator MoveCoroutine(int newX, int newY,float time)
    {
        fruit.X = newX;
        fruit.Y = newY;

        // ÿһ֡�ƶ�һ��
        Vector3 startPos = transform.position;

        Vector3 endPos = fruit.gameManager.FixGridPosition(newX, newY);

        for (float t = 0; t < time; t+= Time.deltaTime)
        {
            // ƽ����֡Vector3.Lerp
            fruit.transform.position = Vector3.Lerp(startPos, endPos,t/time);
            yield return 0;
        }

        // ʱ�����ʱ��δ��ָ��λ�� ��ǿ��λ��
        fruit.transform.position = endPos;
    }
}
