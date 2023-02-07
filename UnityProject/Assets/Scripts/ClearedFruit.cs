using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearedFruit : MonoBehaviour
{
    public AnimationClip clearAnaimation;

    private bool isClearing;

    public bool IsClearing { get => isClearing; }

    protected GameFruit fruit;

    public AudioClip destoryAudio;

    private void Awake()
    {
        fruit = GetComponent<GameFruit>();
    }

    public virtual void Clear()
    {
        isClearing = true;

        StartCoroutine(ClearCoroutine());
    }

    private IEnumerator ClearCoroutine()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(clearAnaimation.name);
            // ��ҵ÷�
            GameManager.GM.playerScore++;
            // ��������
            AudioSource.PlayClipAtPoint(destoryAudio, transform.position);
            yield return new WaitForSeconds(clearAnaimation.length);
            Destroy(gameObject);
        }
    }
}
