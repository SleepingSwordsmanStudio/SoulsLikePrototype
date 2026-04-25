using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Wykrywanie Gracza")]
    public Transform playerTarget;
    public Transform eyeTransform; 
    public float detectionRange = 12f;
    public float viewAngle = 90f;    
    public LayerMask obstructionMask; 

    [Header("Ustawienia Walki")]
    public float attackRange = 1.8f;
    public float attackCooldown = 2.0f;
    public float attackDuration = 0.6f;
    
    [Header("Pauza po Ataku")]
    public float recoveryDuration = 0.5f; // Czas bezruchu po ciosie
    private bool isRecovering = false;

    public WeaponHitbox enemyWeapon;    
    private float lastAttackTime;

    [Header("Ustawienia Trasy")]
    public List<Transform> waypoints;
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 4.5f;
    public float waitTimeAtPoint = 2.0f;

    [Header("Rotacja")]
    public float rotationSpeed = 10.0f;

    private Animator anim;
    private EnemyGravity gravityScript; 
    private PullableObject pullable; 

    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 moveDirection = Vector3.zero;

    void Start()
    {
        anim = GetComponent<Animator>();
        gravityScript = GetComponent<EnemyGravity>(); 
        pullable = GetComponent<PullableObject>();

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (eyeTransform == null) eyeTransform = transform;
        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    void Update()
    {
        moveDirection = Vector3.zero;

        // --- OBSŁUGA TELEKINEZY ---
        if (pullable != null && pullable.isCaptured)
        {
            UpdateAirborneState();
            return; 
        }

        // Blokada logiki podczas odpoczynku po ataku
        if (!isRecovering)
        {
            HandleLogic();
        }

        // Ruch poziomy - sprawdzamy isRecovering, żeby AI nie "ślizgało się" podczas pauzy
        float currentSpeed = (currentState == EnemyState.Chase) ? chaseSpeed : patrolSpeed;
        if (moveDirection.magnitude > 0.1f && !isRecovering)
        {
            transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
        }

        // Grawitacja
        if (gravityScript != null)
        {
            transform.position += Vector3.up * gravityScript.verticalVelocity * Time.deltaTime;
        }

        UpdateAnimator();
    }

    void HandleLogic()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSeePlayer = IsPlayerInFieldOfView();

        if (canSeePlayer && distanceToPlayer <= attackRange)
            currentState = EnemyState.Attack;
        else if (canSeePlayer)
            currentState = EnemyState.Chase;
        else
            currentState = EnemyState.Patrol;

        switch (currentState)
        {
            case EnemyState.Patrol: HandlePatrol(); break;
            case EnemyState.Chase: HandleChase(); break;
            case EnemyState.Attack: HandleAttack(); break;
        }
    }

    void HandlePatrol()
    {
        if (waypoints.Count == 0 || isWaiting) return;
        
        Vector3 target = waypoints[currentWaypointIndex].position;
        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), 
                                     new Vector2(target.x, target.z));

        if (dist < 0.6f)
            StartCoroutine(WaitAtPoint());
        else
            SetMovement(target);
    }

    void HandleChase()
    {
        isWaiting = false; 
        // StopAllCoroutines ucięłoby też Recovery, więc zatrzymujemy tylko WaitAtPoint
        SetMovement(playerTarget.position);
    }

    void SetMovement(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            moveDirection = transform.forward;
        }
    }

    void HandleAttack()
    {
        // Nawet w stanie ataku, patrz na gracza (chyba że trwa recovery)
        if (!isRecovering)
        {
            Vector3 dir = (playerTarget.position - transform.position);
            dir.y = 0;
            if (dir != Vector3.zero) 
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (anim && !anim.GetCurrentAnimatorStateInfo(0).IsTag("AttackTag")) 
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
                
                StartCoroutine(RecoveryRoutine()); // Startujemy pauzę

                if (enemyWeapon != null)
                {
                    enemyWeapon.StartAttack();
                    Invoke("EnemyStopAttack", attackDuration);
                }
            }
        }
    }

    // Korutyna obsługująca bezruch po ataku
    System.Collections.IEnumerator RecoveryRoutine()
    {
        // Czekamy chwilę, aż animacja ataku nabierze impetu (opcjonalne)
        yield return new WaitForSeconds(attackDuration);
        
        isRecovering = true;
        yield return new WaitForSeconds(recoveryDuration);
        isRecovering = false;
    }

    public void EnemyStopAttack() => enemyWeapon?.StopAttack();

    bool IsPlayerInFieldOfView()
    {
        if (playerTarget == null) return false;

        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (dist > detectionRange) return false;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.5f;
        Vector3 dirToPlayer = (targetPoint - eyeTransform.position).normalized;

        if (Vector3.Angle(eyeTransform.forward, dirToPlayer) < viewAngle / 2f)
        {
            RaycastHit hit;
            if (Physics.Raycast(eyeTransform.position, dirToPlayer, out hit, detectionRange, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.CompareTag("Player")) return true;
            }
        }
        
        if (dist < 2.5f) return true;
        return false;
    }

    void UpdateAirborneState()
    {
        if (anim == null) return;
        anim.SetBool("OnGround", false); 
        anim.SetBool("IsWalking", false);
        anim.SetFloat("speed", 0f);
        anim.SetFloat("VerticalVelocity", -5.0f);
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        
        // IsWalking jest true tylko gdy się poruszamy I nie odpoczywamy
        bool isMoving = (moveDirection.magnitude > 0.1f) && !isWaiting && !isRecovering;
        bool grounded = (gravityScript != null && Mathf.Abs(gravityScript.verticalVelocity) < 0.5f);
        
        anim.SetBool("OnGround", grounded);
        anim.SetBool("IsWalking", isMoving);
        
        if (gravityScript != null) anim.SetFloat("VerticalVelocity", gravityScript.verticalVelocity);
        
        float targetSpeedValue = isMoving ? (currentState == EnemyState.Chase ? 1.0f : 0.5f) : 0f;
        anim.SetFloat("speed", targetSpeedValue, 0.1f, Time.deltaTime);
    }

    System.Collections.IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        yield return new WaitForSeconds(waitTimeAtPoint);
        isWaiting = false;
    }

    void OnDrawGizmosSelected()
    {
        if (eyeTransform == null) return;
        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * eyeTransform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * eyeTransform.forward;
        Gizmos.DrawRay(eyeTransform.position, left * detectionRange);
        Gizmos.DrawRay(eyeTransform.position, right * detectionRange);
    }
}