using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class BonfireUIManager : MonoBehaviour
{
    public static event Action OnPlayerRest;

    [Header("UI Panels")]
    public GameObject bonfireMenuPanel;
    public GameObject respawnMessage;
    public GameObject bonfireLitPanel;

    [Header("Buttons")]
    public Button restButton;
    public Button fastTravelButton;
    public Button standUpButton;

    [Header("Fast Travel Settings")]
    public FastTravelUI fastTravelSystem;

    [Header("Settings")]
    public float litMessageDuration = 3f;

    private BonfireInteraction currentBonfire;

    void Start()
    {
        if (bonfireMenuPanel != null) bonfireMenuPanel.SetActive(false);
        if (respawnMessage != null) respawnMessage.SetActive(false);
        if (bonfireLitPanel != null) bonfireLitPanel.SetActive(false);

        if (restButton != null)
            restButton.onClick.AddListener(OnRestClicked);

        if (fastTravelButton != null && fastTravelSystem != null)
            fastTravelButton.onClick.AddListener(OnFastTravelClicked);

        if (standUpButton != null)
            standUpButton.onClick.AddListener(OnStandUpClicked);
    }

    public void OpenMenu(BonfireInteraction bonfire)
    {
        currentBonfire = bonfire;
        bonfireMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        if (bonfireMenuPanel != null) bonfireMenuPanel.SetActive(false);

        if (fastTravelSystem != null && fastTravelSystem.fastTravelPanel != null)
            fastTravelSystem.fastTravelPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowBonfireLitMessage()
    {
        if (bonfireLitPanel != null)
        {
            StartCoroutine(BonfireLitRoutine());
        }
    }

    private IEnumerator BonfireLitRoutine()
    {
        bonfireLitPanel.SetActive(true);
        CanvasGroup cg = bonfireLitPanel.GetComponent<CanvasGroup>();
        
        if (cg != null)
        {
            cg.alpha = 0;
            while (cg.alpha < 1)
            {
                cg.alpha += Time.deltaTime * 2f;
                yield return null;
            }
        }

        yield return new WaitForSeconds(litMessageDuration);

        if (cg != null)
        {
            while (cg.alpha > 0)
            {
                cg.alpha -= Time.deltaTime * 1f;
                yield return null;
            }
        }

        bonfireLitPanel.SetActive(false);
    }

    void OnRestClicked()
    {
        OnPlayerRest?.Invoke();

        if (respawnMessage != null)
        {
            StopCoroutine("ShowRespawnMessage");
            StartCoroutine(ShowRespawnMessage());
        }
    }

    void OnFastTravelClicked()
    {
        if (fastTravelSystem != null)
        {
            fastTravelSystem.OpenFastTravel();
        }
    }

    void OnStandUpClicked()
    {
        if (currentBonfire != null)
        {
            currentBonfire.RequestStandUp();
            CloseMenu();
        }
    }

    IEnumerator ShowRespawnMessage()
    {
        respawnMessage.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        respawnMessage.SetActive(false);
    }

    public BonfireInteraction GetCurrentBonfire()
    {
        return currentBonfire;
    }
}