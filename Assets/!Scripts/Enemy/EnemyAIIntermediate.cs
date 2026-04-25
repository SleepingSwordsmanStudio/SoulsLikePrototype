using UnityEngine;
using System.Collections.Generic;

public class EnemyAIIntermediate : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Orbiting }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Wykrywanie Gracza")]
    public Transform playerTarget;
    public Transform eyeTransform; 
    public float detectionRange = 15f;
    public float viewAngle = 110f;    
    public LayerMask obstructionMask; 

    [Header("Ustawienia Walki & Krążenia")]
    public float attackRange = 2.5f;      
    public float orbitDistance = 4.5f;    
    public float orbitSpeed = 1.25f;      
    
    [Header("Losowy Cooldown Ataku")]
    public float minAttackCooldown = 1.5f;
    public float maxAttackCooldown = 4.0f;
    private float currentRandomCooldown;

    [Header("Pauza po Ataku")]
    public float recoveryDuration = 0.5f; // Czas bezruchu po ataku
    private bool isRecovering = false;

    public float attackDuration = 0.7f;
    public WeaponHitbox enemyWeapon;    
    
    [Header("Ustawienia Trasy")]
    public List<Transform> waypoints;
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 5.5f;       
    public float waitTimeAtPoint = 2.0f;

    [Header("Rotacja")]
    public float rotationSpeed = 15.0f;

    private Animator anim;
    private EnemyGravity gravityScript; 
    private PullableObject pullable; 
    
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 moveDirection = Vector3.zero;
    private float lastAttackTime;
    private float orbitDirection = 1f; 
    private float nextOrbitDirChange;
    private bool isDashing = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        gravityScript = GetComponent<EnemyGravity>(); 
        pullable = GetComponent<PullableObject>();

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (eyeTransform == null) eyeTransform = transform;
        currentRandomCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    void Update()
    {
        if (pullable != null && pullable.isCaptured)
        {
            UpdateAirborneState();
            return; 
        }

        // Jeśli szkielet odpoczywa po ciosie, nie wykonuj logiki ruchu
        if (!isRecovering)
        {
            HandleLogic();
        }
        else
        {
            moveDirection = Vector3.zero; // Stój w miejscu podczas recovery
        }

        float currentSpeed = patrolSpeed;
        if (currentState == EnemyState.Chase) currentSpeed = chaseSpeed;
        else if (currentState == EnemyState.Orbiting) currentSpeed = orbitSpeed;
        else if (currentState == EnemyState.Attack) currentSpeed = chaseSpeed * 0.3f;

        // Ruch poziomy - zablokowany podczas recovery lub dasha
        if (moveDirection.magnitude > 0.1f && !isDashing && !isRecovering)
        {
            ApplySafeMovement(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }

        if (gravityScript != null && !isDashing)
        {
            float clampedVel = Mathf.Max(gravityScript.verticalVelocity, -15f);
            Vector3 gravityMove = Vector3.up * clampedVel * Time.deltaTime;
            transform.position += gravityMove;
            ApplySafeMovement(Vector3.zero);
        }

        UpdateAnimator();
    }

    void ApplySafeMovement(Vector3 delta)
    {
        transform.position += delta;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f, obstructionMask))
        {
            if (transform.position.y < hit.point.y)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
        }
    }

    void HandleLogic()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSeePlayer = IsPlayerInFieldOfView();
        bool attackReady = Time.time >= lastAttackTime + currentRandomCooldown;

        if (canSeePlayer)
        {
            if (attackReady)
            {
                if (distanceToPlayer <= attackRange)
                    currentState = EnemyState.Attack;
                else
                    currentState = EnemyState.Chase; 
            }
            else
            {
                if (distanceToPlayer <= orbitDistance + 1f)
                    currentState = EnemyState.Orbiting;
                else
                    currentState = EnemyState.Chase;
            }
        }
        else
        {
            currentState = EnemyState.Patrol;
        }

        switch (currentState)
        {
            case EnemyState.Patrol: HandlePatrol(); break;
            case EnemyState.Chase: HandleChase(); break;
            case EnemyState.Orbiting: HandleOrbiting(); break;
            case EnemyState.Attack: HandleAttack(); break;
        }
    }

    void HandleOrbiting()
    {
        LookAtTarget(playerTarget.position);

        if (Time.time > nextOrbitDirChange)
        {
            orbitDirection *= -1;
            nextOrbitDirChange = Time.time + Random.Range(2f, 4f);
        }

        Vector3 dirToPlayer = (transform.position - playerTarget.position).normalized;
        Vector3 lateralDir = Vector3.Cross(dirToPlayer, Vector3.up) * orbitDirection;
        
        Vector3 radialDir = Vector3.zero;
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (dist < orbitDistance - 0.5f) radialDir = dirToPlayer; 
        else if (dist > orbitDistance + 0.5f) radialDir = -dirToPlayer;

        moveDirection = (lateralDir + radialDir * 0.5f).normalized;
    }

    void HandleAttack()
    {
        LookAtTarget(playerTarget.position);
        
        if (Time.time >= lastAttackTime + currentRandomCooldown)
        {
            if (anim && !anim.GetCurrentAnimatorStateInfo(0).IsTag("AttackTag")) 
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
                currentRandomCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
                
                StartCoroutine(AttackRoutine()); // Zmieniona korutyna

                if (enemyWeapon != null)
                {
                    enemyWeapon.StartAttack();
                    Invoke("EnemyStopAttack", attackDuration);
                }
            }
        }
    }

    // Nowa korutyna łącząca Dash i Recovery
    System.Collections.IEnumerator AttackRoutine()
    {
        // 1. FAZA DASH (Atak)
        isDashing = true;
        if(gravityScript != null) gravityScript.verticalVelocity = 0; 

        float dashTime = 0.35f; 
        float timer = 0f;
        while (timer < dashTime)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distToPlayer > 1.2f)
            {
                Vector3 dashDelta = transform.forward * 8.5f * Time.deltaTime;
                ApplySafeMovement(dashDelta);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        isDashing = false;

        // 2. FAZA RECOVERY (Pauza po ataku)
        isRecovering = true;
        moveDirection = Vector3.zero;
        yield return new WaitForSeconds(recoveryDuration);
        isRecovering = false;
    }

    public void EnemyStopAttack() => enemyWeapon?.StopAttack();

    void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }

    bool IsPlayerInFieldOfView()
    {
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (dist > detectionRange) return false;
        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.5f;
        Vector3 dir = (targetPoint - eyeTransform.position).normalized;
        if (Vector3.Angle(eyeTransform.forward, dir) < viewAngle / 2f)
        {
            if (Physics.Raycast(eyeTransform.position, dir, out RaycastHit hit, detectionRange, obstructionMask, QueryTriggerInteraction.Ignore))
                if (hit.transform.CompareTag("Player")) return true;
        }
        return dist < 4f; 
    }

    void UpdateAirborneState()
    {
        if (anim == null) return;
        anim.SetBool("OnGround", false); 
        anim.SetBool("IsWalking", false);
        anim.SetFloat("speed", 0f);
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        
        bool isMoving = moveDirection.magnitude > 0.1f;
        bool isFalling = gravityScript != null && gravityScript.verticalVelocity < -2.0f;
        bool grounded = !isFalling;

        anim.SetBool("OnGround", grounded);
        // Podczas recovery IsWalking będzie false, co odpali animację Idle
        anim.SetBool("IsWalking", isMoving && !isRecovering);
        
        float speedVal = (currentState == EnemyState.Chase || isDashing) ? 1.0f : (isMoving ? 0.5f : 0f);
        if (isRecovering) speedVal = 0f;

        anim.SetFloat("speed", speedVal, 0.1f, Time.deltaTime);
        anim.SetFloat("VerticalVelocity", isFalling ? gravityScript.verticalVelocity : 0f);
    }

    void HandlePatrol()
    {
        if (waypoints.Count == 0 || isWaiting) return;
        Vector3 target = waypoints[currentWaypointIndex].position;
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.x, target.z)) < 0.8f)
            StartCoroutine(WaitAtPoint());
        else
        {
            LookAtTarget(target);
            moveDirection = transform.forward;
        }
    }

    void HandleChase()
    {
        isWaiting = false;
        LookAtTarget(playerTarget.position);
        moveDirection = transform.forward;
    }

    System.Collections.IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        if (waypoints.Count > 0)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        yield return new WaitForSeconds(waitTimeAtPoint);
        isWaiting = false;
    }
}