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
    public float recoveryDuration = 0.5f; 
    private bool isRecovering = false;

    [Header("Ustawienia Detekcji Ziemi")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

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
    private Rigidbody rb; 
    private PullableObject pullable; 
    
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float lastAttackTime;
    private float orbitDirection = 1f; 
    private float nextOrbitDirChange;
    private bool isDashing = false;

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");
    private readonly int hashSpeed = Animator.StringToHash("speed");

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>(); 
        pullable = GetComponent<PullableObject>();

        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default");
        if (playerTarget == null) playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (eyeTransform == null) eyeTransform = transform;
        
        currentRandomCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    void FixedUpdate()
    {
        if (pullable != null && pullable.isCaptured)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            UpdateAirborneState();
            return;
        }

        rb.useGravity = !isDashing;
        bool grounded = CheckIfGrounded();

        if (isRecovering)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false, 0f);
            return;
        }

        if (!isDashing)
        {
            HandleLogic(grounded);
        }
    }

    void HandleLogic(bool grounded)
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSeePlayer = IsPlayerInFieldOfView();
        bool attackReady = Time.time >= lastAttackTime + currentRandomCooldown;

        if (canSeePlayer)
        {
            if (attackReady)
            {
                currentState = (distanceToPlayer <= attackRange) ? EnemyState.Attack : EnemyState.Chase;
            }
            else
            {
                currentState = (distanceToPlayer <= orbitDistance + 1f) ? EnemyState.Orbiting : EnemyState.Chase;
            }
        }
        else
        {
            currentState = EnemyState.Patrol;
        }

        switch (currentState)
        {
            case EnemyState.Patrol: HandlePatrol(grounded); break;
            case EnemyState.Chase: HandleChase(grounded); break;
            case EnemyState.Orbiting: HandleOrbiting(grounded); break;
            case EnemyState.Attack: HandleAttack(grounded); break;
        }
    }

    void HandlePatrol(bool grounded)
    {
        if (waypoints.Count == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false, 0f);
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0) isWaiting = false;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false, 0f);
            return;
        }

        Vector3 target = waypoints[currentWaypointIndex].position;
        Vector3 diff = target - transform.position;
        diff.y = 0;

        if (diff.magnitude < 0.8f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else
        {
            ApplyMovement(diff, patrolSpeed, grounded, 0.5f);
        }
    }

    void HandleChase(bool grounded)
    {
        isWaiting = false;
        Vector3 diff = playerTarget.position - transform.position;
        diff.y = 0;
        ApplyMovement(diff, chaseSpeed, grounded, 1.0f);
    }

    void HandleOrbiting(bool grounded)
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

        Vector3 finalDir = (lateralDir + radialDir * 0.5f).normalized;
        ApplyMovement(finalDir, orbitSpeed, grounded, 0.5f);
    }

    void HandleAttack(bool grounded)
    {
        LookAtTarget(playerTarget.position);
        
        if (Time.time >= lastAttackTime + currentRandomCooldown)
        {
            if (anim && !anim.GetCurrentAnimatorStateInfo(0).IsTag("AttackTag")) 
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
                currentRandomCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
                StartCoroutine(AttackRoutine());

                if (enemyWeapon != null)
                {
                    enemyWeapon.StartAttack();
                    Invoke("EnemyStopAttack", attackDuration);
                }
            }
        }
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        UpdateAnimator(grounded, rb.linearVelocity.y, false, 0f);
    }

    System.Collections.IEnumerator AttackRoutine()
    {
        isDashing = true;
        float dashTime = 0.35f; 
        float timer = 0f;
        while (timer < dashTime)
        {
            if (Vector3.Distance(transform.position, playerTarget.position) > 1.2f)
            {
                rb.linearVelocity = transform.forward * 8.5f;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        isDashing = false;
        isRecovering = true;
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(recoveryDuration);
        isRecovering = false;
    }

    void ApplyMovement(Vector3 direction, float mSpeed, bool grounded, float animSpeed)
    {
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
            Vector3 vel = transform.forward * mSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
            UpdateAnimator(grounded, rb.linearVelocity.y, true, animSpeed);
        }
    }

    void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.fixedDeltaTime));
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

    bool CheckIfGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }

    void UpdateAirborneState()
    {
        if (anim == null) return;
        anim.SetBool(hashOnGround, false); 
        anim.SetBool(hashIsWalking, false);
        anim.SetFloat(hashSpeed, 0f);
        anim.SetFloat(hashVertVel, -5.0f);
    }

    void UpdateAnimator(bool grounded, float vVel, bool isMoving, float speedVal)
    {
        if (anim == null) return;
        anim.SetBool(hashOnGround, grounded);
        anim.SetBool(hashIsWalking, isMoving);
        anim.SetFloat(hashVertVel, vVel);
        anim.SetFloat(hashSpeed, speedVal, 0.1f, Time.fixedDeltaTime);
    }

    public void EnemyStopAttack() => enemyWeapon?.StopAttack();

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