using UnityEngine;
using System.Collections.Generic;

public class LockOnSystem : MonoBehaviour
{
    [Header("Required References")]
    public PlayerInput input;
    public PlayerDodgeSystem dodgeSystem;
    public TelekinesisSystem teleSystem; // Przypisz w inspektorze lub znajdzie w Awake

    [Header("Settings")]
    public float lockRadius = 15f;
    public LayerMask enemyLayer;
    public float rotationSpeed = 10f;

    [Header("Deadzone Settings")]
    [Tooltip("Standardowa tolerancja obrotu (w stopniach)")]
    public float normalDeadzone = 1f;    
    [Tooltip("Tolerancja podczas przyciągania - zapobiega kręceniu kółek przy bocznym anchorze")]
    public float telekinesisDeadzone = 25f; 

    [Header("Targeting State (Read Only)")]
    public Transform currentTarget;
    public bool IsLockedOn => currentTarget != null;

    private void Awake()
    {
        if (teleSystem == null) teleSystem = GetComponent<TelekinesisSystem>();
    }

    /// <summary>
    /// Główna logika – wywoływana przez PlayerBrain w Update()
    /// </summary>
    public void Tick()
    {
        if (input == null) return;

        // Przełączanie Lock-On (Klawisz Q)
        if (input.LockOnDown)
        {
            if (!IsLockedOn) FindTarget();
            else Unlock();
        }

        // Zmiana celu (Lewy Alt)
        if (input.SwitchTargetDown && IsLockedOn)
        {
            SwitchTarget();
        }

        // Automatyczne odpięcie, jeśli cel zniknie lub odejdzie za daleko
        if (IsLockedOn)
        {
            if (currentTarget == null)
            {
                Unlock();
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > lockRadius + 5f)
            {
                Unlock();
            }
        }
    }

    /// <summary>
    /// Metoda rotacji – wywoływana wewnątrz PlayerMovement.ExecutePhysics.
    /// Uwzględnia Deadzone dla telekinezy.
    /// </summary>
    public void RotatePlayerTowardsTarget()
    {
        if (currentTarget == null) return;
        
        // Nie obracaj postaci do wroga, jeśli właśnie wykonuje Roll
        if (dodgeSystem != null && dodgeSystem.IsDodging) return;

        Vector3 dir = Vector3.ProjectOnPlane(currentTarget.position - transform.position, Vector3.up).normalized;
        
        if (dir != Vector3.zero)
        {
            // Obliczamy kąt między obecnym przodem gracza a kierunkiem na cel
            float angleToTarget = Vector3.Angle(transform.forward, dir);

            // Wybieramy margines błędu w zależności od stanu telekinezy
            float currentDeadzone = (teleSystem != null && teleSystem.IsCurrentlyPulling) 
                                    ? telekinesisDeadzone 
                                    : normalDeadzone;

            // Obracaj gracza tylko, jeśli cel wyjdzie poza martwą strefę (Deadzone)
            if (angleToTarget > currentDeadzone)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed);
            }
        }
    }

    private void FindTarget()
    {
        SetTarget(GetClosestEnemy());
    }

    private void SwitchTarget()
    {
        Transform nextTarget = GetClosestEnemy(currentTarget);
        if (nextTarget != null) SetTarget(nextTarget);
    }

    private void SetTarget(Transform newTarget)
    {
        // Wyłączenie starego wskaźnika
        if (currentTarget != null && currentTarget.TryGetComponent(out EnemyTarget oldEt)) 
            oldEt.ToggleIndicator(false);
        
        currentTarget = newTarget;

        // Włączenie nowego wskaźnika
        if (currentTarget != null && currentTarget.TryGetComponent(out EnemyTarget newEt)) 
            newEt.ToggleIndicator(true);
    }

    private Transform GetClosestEnemy(Transform exception = null)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, lockRadius, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform best = null;

        foreach (var enemy in enemies)
        {
            if (exception != null && enemy.transform == exception) continue;

            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                best = enemy.transform;
            }
        }
        return best;
    }

    public void Unlock()
    {
        SetTarget(null);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lockRadius);
    }
}