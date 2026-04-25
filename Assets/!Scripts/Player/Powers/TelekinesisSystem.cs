using UnityEngine;

public class TelekinesisSystem : MonoBehaviour
{
    [Header("Systemy Zależne")]
    public LockOnSystem lockOn;
    private PlayerInput playerInput;
    private PlayerDodgeSystem dodgeSystem;

    [Header("Referencje Mocy")]
    public PushPower pushPower;
    public PullPower pullPower;

    [Header("Globalne Ustawienia")]
    public float range = 15f;
    [Range(0, 180)] public float viewAngle = 60f;
    public LayerMask enemyLayer;
    public LayerMask projectileLayer;

    public bool IsCurrentlyPulling => pullPower != null && pullPower.IsActive;

    void Awake()
    {
        // Szukamy w rodzicu (Player), bo tam masz te skrypty na screenie
        playerInput = GetComponentInParent<PlayerInput>();
        dodgeSystem = GetComponentInParent<PlayerDodgeSystem>();
        
        // Jeśli LockOn jest na tym samym obiekcie co Movement, też szukamy w rodzicu
        if (lockOn == null) lockOn = GetComponentInParent<LockOnSystem>();
    }

    void Start()
    {
        if (pushPower) pushPower.Initialize(this);
        if (pullPower) pullPower.Initialize(this);
    }

    void Update()
    {
        // Sprawdzenie, czy udało się znaleźć Input w rodzicu
        if (playerInput == null) return;

        // Blokada mocy podczas uniku
        if (dodgeSystem != null && dodgeSystem.IsDodging) return;

        if (pushPower) 
            pushPower.ProcessInput(playerInput.PushDown, playerInput.PushHold, playerInput.PushUp);
        
        if (pullPower) 
            pullPower.ProcessInput(playerInput.PullDown, playerInput.PullHold, playerInput.PullUp);
    }

    public Transform GetCurrentTarget(LayerMask mask)
    {
        if (lockOn != null && lockOn.currentTarget != null) 
        {
            if (((1 << lockOn.currentTarget.gameObject.layer) & mask) != 0)
                return lockOn.currentTarget;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, range, mask);
        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Vector3 dir = (hit.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) < viewAngle / 2f)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = hit.transform;
                }
            }
        }
        return bestTarget;
    }
}