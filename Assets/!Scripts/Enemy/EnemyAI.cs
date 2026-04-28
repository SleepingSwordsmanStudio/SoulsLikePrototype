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
    public float recoveryDuration = 0.5f;

    [Header("Ustawienia Trasy")]
    public List<Transform> waypoints;
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 4.5f;
    public float waitTimeAtPoint = 2.0f;

    [Header("Detekcja Ziemi")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    [Header("Rotacja")]
    public float rotationSpeed = 10.0f;

    public WeaponHitbox enemyWeapon;    
    private float lastAttackTime;
    private bool isRecovering = false;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    private Animator anim;
    private Rigidbody rb;
    private PullableObject pullable;

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

        rb.useGravity = true;
        bool grounded = CheckIfGrounded();

        if (isRecovering)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
            return;
        }

        HandleLogic(grounded);
    }

    void HandleLogic(bool grounded)
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
            case EnemyState.Patrol: HandlePatrol(grounded); break;
            case EnemyState.Chase: HandleChase(grounded); break;
            case EnemyState.Attack: HandleAttack(grounded); break;
        }
    }

    void HandlePatrol(bool grounded)
    {
        if (waypoints.Count == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0) isWaiting = false;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
            return;
        }

        Vector3 target = waypoints[currentWaypointIndex].position;
        Vector3 diff = target - transform.position;
        diff.y = 0;

        if (diff.magnitude < 0.6f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
        }
        else
        {
            ApplyMovement(diff, patrolSpeed, grounded);
        }
    }

    void HandleChase(bool grounded)
    {
        isWaiting = false;
        Vector3 diff = playerTarget.position - transform.position;
        diff.y = 0;
        ApplyMovement(diff, chaseSpeed, grounded);
    }

    void ApplyMovement(Vector3 direction, float moveSpeed, bool grounded)
    {
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
            Vector3 vel = transform.forward * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
            UpdateAnimator(grounded, rb.linearVelocity.y, true);
        }
    }

    void HandleAttack(bool grounded)
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        
        Vector3 dir = (playerTarget.position - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.fixedDeltaTime));

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (anim && !anim.GetCurrentAnimatorStateInfo(0).IsTag("AttackTag"))
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
                StartCoroutine(RecoveryRoutine());

                if (enemyWeapon != null)
                {
                    enemyWeapon.StartAttack();
                    Invoke("EnemyStopAttack", attackDuration);
                }
            }
        }
        UpdateAnimator(grounded, rb.linearVelocity.y, false);
    }

    System.Collections.IEnumerator RecoveryRoutine()
    {
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
        return dist < 2.5f;
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

    void UpdateAnimator(bool grounded, float vVel, bool isMoving)
    {
        if (anim == null) return;
        anim.SetBool(hashOnGround, grounded);
        anim.SetBool(hashIsWalking, isMoving);
        anim.SetFloat(hashVertVel, vVel);
        float targetSpeedValue = isMoving ? (currentState == EnemyState.Chase ? 1.0f : 0.5f) : 0f;
        anim.SetFloat(hashSpeed, targetSpeedValue, 0.1f, Time.fixedDeltaTime);
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