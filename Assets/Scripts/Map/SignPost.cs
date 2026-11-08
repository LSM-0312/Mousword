using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SignPost : MonoBehaviour
{
    [Header("UI오브젝트 연결")]
    [SerializeField] private GameObject bubbleUI;
    [SerializeField] private Image image;

    [Header("UI내부 그림")]
    [SerializeField] private Sprite[] signFrames;
    [SerializeField] private float frameInterval = 0.4f;

    private Coroutine animCoroutine;

    private void Start()
    {
        if(bubbleUI != null)
        {
            bubbleUI.SetActive(false);
        }

        InitSignContent();
    }

    private void InitSignContent()
    {
        if(image == null)
        {
            return;
        }

        bool hasFrames = signFrames != null && signFrames.Length > 0;
        image.gameObject.SetActive(hasFrames);

        if(hasFrames)
        {
            image.sprite = signFrames[0];
        }
    }

    private void ShowBubble(bool show)
    {
        if(bubbleUI != null)
        {
            bubbleUI.SetActive(show);
        }

        if(animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if(show && image != null && signFrames != null && signFrames.Length > 1)
        {
            animCoroutine = StartCoroutine(PlayFrames());
        }
    }

    private IEnumerator PlayFrames()
    {
        int index = 0;
        WaitForSeconds wait = new WaitForSeconds(frameInterval);

        while(true)
        {
            image.sprite = signFrames[index];
            index = (index + 1) % signFrames.Length;
            yield return wait;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ShowBubble(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ShowBubble(false);
        }
    }
}
