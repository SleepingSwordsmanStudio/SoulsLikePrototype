using UnityEngine;

public class PullPower : MonoBehaviour
{
    [Header("Ustawienia")]
    public Transform pullAnchor; 
    
    private TelekinesisSystem system;
    private PullableObject currentTarget;
    private Rigidbody targetRb;

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

            if (targetRb != null)
            {
                targetRb.useGravity = false;
                targetRb.linearVelocity = Vector3.zero;
                targetRb.angularVelocity = Vector3.zero;
            }

            currentTarget.StartPull(pullAnchor);
        }
    }

    void StopPulling()
    {
        IsActive = false;

        if (targetRb != null)
        {
            targetRb.useGravity = true;
            targetRb = null;
        }

        if (currentTarget != null)
        {
            currentTarget.StopPull();
            currentTarget = null;
        }
    }
}