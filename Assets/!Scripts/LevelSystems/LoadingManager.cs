using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Elements")]
    public CanvasGroup canvasGroup; // Główny panel (do Fading)
    public Image backgroundImage;   // Tło (czarna grafika)
    public TMP_Text loadingText;    // Napis "Loading..."

    [Header("Settings")]
    public float fadeSpeed = 2f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicjalizacja: niewidoczne i nie blokuje myszki
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = true;
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    public IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.blocksRaycasts = false;
    }

    // Opcjonalnie: metoda do zmiany tekstu podpowiedzi
    public void SetLoadingText(string text)
    {
        if (loadingText != null) loadingText.text = text;
    }
}