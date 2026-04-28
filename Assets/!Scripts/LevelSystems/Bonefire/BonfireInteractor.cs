using UnityEngine;
using TMPro;
using System.Collections;

public class BonfireInteraction : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private Collider detectionTrigger;
    [SerializeField] private TextMeshPro interactionText;
    [SerializeField] private BonfireUIManager uiManager;

    [Header("Ustawienia Animacji")]
    [SerializeField] private float animationDuration = 1.5f;

    [Header("Teleport Settings (Fast Travel)")]
    public string bonfireName = "Bonfire Name";

    [Header("Visuals")]
    public Animator bonfireAnimator;

    private bool isPlayerInside = false;
    private bool isMeditating = false;
    private bool isTransitioning = false;
    public bool isDiscovered = false;
    private Animator playerAnimator;
    private CharacterController playerCC;
    private Transform cameraTransform;

    void Start()
    {
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (uiManager == null)
            uiManager = FindFirstObjectByType<BonfireUIManager>();

        if (interactionText != null) 
            interactionText.gameObject.SetActive(false);
            
        if (detectionTrigger != null) 
            detectionTrigger.isTrigger = true;
    }

    void Update()
    {
        if (isPlayerInside && !isTransitioning)
        {
            if (!isMeditating) RotateTextToCamera();

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(HandleMeditationRoutine());
            }
        }
    }

    private IEnumerator HandleMeditationRoutine()
    {
        isTransitioning = true;
        isMeditating = !isMeditating;

        if (interactionText != null) 
            interactionText.gameObject.SetActive(false);

        if (playerAnimator != null)
            playerAnimator.SetBool("IsMeditating", isMeditating);

        yield return new WaitForSeconds(animationDuration);

        if (isMeditating)
        {
            if (playerCC != null) playerCC.enabled = false;
            
            if (!isDiscovered)
            {
                isDiscovered = true;
                if (uiManager != null) uiManager.ShowBonfireLitMessage();
                if (bonfireAnimator != null) bonfireAnimator.SetTrigger("OnFirstLit");
            }

            if (BonfireManager.Instance != null)
                BonfireManager.Instance.RegisterBonfire(this);
            
            if (uiManager != null)
                uiManager.OpenMenu(this);
        }
        else
        {
            if (playerCC != null) playerCC.enabled = true;
            
            if (uiManager != null)
                uiManager.CloseMenu();

            if (isPlayerInside && interactionText != null) 
                interactionText.gameObject.SetActive(true);
        }

        isTransitioning = false;
    }

    public void RequestStandUp()
    {
        if (!isTransitioning && isMeditating)
        {
            StartCoroutine(HandleMeditationRoutine());
        }
    }

    private void RotateTextToCamera()
    {
        if (interactionText != null && cameraTransform != null)
        {
            interactionText.transform.LookAt(interactionText.transform.position + cameraTransform.rotation * Vector3.forward,
                                             cameraTransform.rotation * Vector3.up);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (!isMeditating && !isTransitioning && interactionText != null) 
                interactionText.gameObject.SetActive(true);
            
            playerAnimator = other.GetComponent<Animator>();
            playerCC = other.GetComponent<CharacterController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (interactionText != null) 
                interactionText.gameObject.SetActive(false);
        }
    }

    public void SetMeditatingDirectly(bool state)
    {
        isMeditating = state;
        isDiscovered = true; 
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            if (playerAnimator == null) playerAnimator = player.GetComponent<Animator>();
            if (playerCC == null) playerCC = player.GetComponent<CharacterController>();

            if (playerAnimator != null)
                playerAnimator.SetBool("IsMeditating", state);

            if (playerCC != null) playerCC.enabled = !state;
        }
    }
}