using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class FastTravelUI : MonoBehaviour
{
    [Header("Ustawienia Panelu")]
    public GameObject fastTravelPanel;
    public Transform buttonContainer;

    [Header("Prefab")]
    public GameObject locationButtonPrefab;

    private BonfireUIManager uiManager;

    void Awake()
    {
        uiManager = GetComponentInParent<BonfireUIManager>();
        if (fastTravelPanel != null)
            fastTravelPanel.SetActive(false);
    }

    public void OpenFastTravel()
    {
        if (fastTravelPanel == null) return;
        fastTravelPanel.SetActive(true);
        GenerateLocationButtons();
    }

    private void GenerateLocationButtons()
    {
        foreach (Transform child in buttonContainer) 
        {
            Destroy(child.gameObject);
        }

        List<BonfireInteraction> allBonfires = BonfireManager.Instance.discoveredBonfires;
        BonfireInteraction current = uiManager.GetCurrentBonfire();

        foreach (BonfireInteraction bonfire in allBonfires)
        {
            if (current != null && bonfire.bonfireName == current.bonfireName) 
                continue;

            GameObject newBtn = Instantiate(locationButtonPrefab, buttonContainer);
            
            TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = bonfire.bonfireName;

            BonfireInteraction target = bonfire;
            Button btn = newBtn.GetComponent<Button>();
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => TeleportTo(target));
        }
    }

    private void TeleportTo(BonfireInteraction target)
    {
        StartCoroutine(TeleportRoutine(target));
    }

    private IEnumerator TeleportRoutine(BonfireInteraction target)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        if (LoadingManager.Instance != null)
            yield return StartCoroutine(LoadingManager.Instance.FadeIn());

        uiManager.CloseMenu();
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;
        player.transform.position = target.transform.position + Vector3.up * 0.2f;
        player.transform.rotation = target.transform.rotation;
        if (cc != null) cc.enabled = true;

        target.SetMeditatingDirectly(true);
        uiManager.OpenMenu(target);

        yield return new WaitForSeconds(0.5f);

        if (LoadingManager.Instance != null)
            yield return StartCoroutine(LoadingManager.Instance.FadeOut());

        Debug.Log("Przeniesiono do: " + target.bonfireName);
    }

    public void CloseFastTravel() => fastTravelPanel.SetActive(false);
}