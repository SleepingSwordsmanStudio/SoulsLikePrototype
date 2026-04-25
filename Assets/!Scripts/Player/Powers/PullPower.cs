using UnityEngine;

public class PullPower : MonoBehaviour
{
    [Header("Ustawienia")]
    public Transform pullAnchor; 
    
    private TelekinesisSystem system;
    private PullableObject currentTarget;
    private Rigidbody targetRb;
    private EnemyGravity targetGravity; // DODANE: Referencja do skryptu grawitacji

    public bool IsActive { get; private set; }

    public void Initialize(TelekinesisSystem sys) => system = sys;

    public void ProcessInput(bool down, bool held, bool up)
    {
        if (down && system.lockOn != null && system.lockOn.currentTarget != null) 
        {
            StartPulling(system.lockOn.currentTarget);
        }

        if (IsActive)
        {
            if (up || !held || system.lockOn == null || system.lockOn.currentTarget == null)
            {
                StopPulling();
            }
        }
    }

    void StartPulling(Transform target)
    {
        currentTarget = target.GetComponent<PullableObject>();
        
        if (currentTarget != null)
        {
            IsActive = true;
            targetRb = target.GetComponent<Rigidbody>();
            targetGravity = target.GetComponent<EnemyGravity>(); // Szukamy skryptu grawitacji

            // 1. ZATRZYMANIE FIZYKI RIGIDBODY
            if (targetRb != null)
            {
                targetRb.useGravity = false;
                targetRb.linearVelocity = Vector3.zero; // Pełne zatrzymanie pędu
            }

            // 2. ZATRZYMANIE SKRYPTU ENEMYGRAVITY
            if (targetGravity != null)
            {
                targetGravity.verticalVelocity = 0f;
                targetGravity.enabled = false; // WYŁĄCZAMY skrypt, żeby nie przesuwał transform.position
            }

            currentTarget.StartPull(pullAnchor);
        }
    }

    void StopPulling()
    {
        IsActive = false;

        // PRZYWRACANIE RIGIDBODY
        if (targetRb != null)
        {
            targetRb.useGravity = true;
            targetRb = null;
        }

        // PRZYWRACANIE ENEMYGRAVITY
        if (targetGravity != null)
        {
            targetGravity.enabled = true; // Włączamy z powrotem, wróg zacznie spadać
            targetGravity = null;
        }

        if (currentTarget != null)
        {
            currentTarget.StopPull();
            currentTarget = null;
        }
    }
}