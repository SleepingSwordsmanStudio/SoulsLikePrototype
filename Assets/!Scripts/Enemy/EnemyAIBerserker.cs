using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAIBerserker : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Attack, Stagger, Retreat, Dead }

    [Header("Stan AI")]
    public EnemyState currentState = EnemyState.Idle;

    [Header("Wykrywanie Gracza")]
    public Transform playerTarget;
    public float detectionRange = 15f;
    public float rotationSpeed = 12f;

    [Header("Ustawienia Walki")]
    public float attackRange = 2.2f;
    public float attackCooldown = 1.5f;
    public float comboChance = 0.5f; 
    public WeaponHitbox enemyWeapon;
    
    [Header("Ustawienia Animacji")]
    public string staggerTrigger = "Stagger";
    public string flinchTrigger = "Flinch";
    public float staggerDuration = 2.0f;

    [Header("Ruch")]
    public float chaseSpeed = 5.5f;
    public float retreatSpeed = 3.0f;
    public float attackDashForce = 7f;

    [Header("Detekcja Ziemi")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    private Animator anim;
    private Rigidbody rb;
    private PullableObject pullable;
    private float lastAttackTime;
    private bool canAction = true;
    private bool isDead = false;

    private readonly int hashSpeed = Animator.StringToHash("speed");
    private readonly int hashIsStaggered = Animator.StringToHash("IsStaggered");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        pullable = GetComponent<PullableObject>();

        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default");
        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (pullable != null && pullable.isCaptured)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            UpdateAirborneState();
            return;
        }

        rb.useGravity = true;
        bool grounded = CheckIfGrounded();

        if (!canAction || playerTarget == null)
        {
            if (currentState != EnemyState.Attack)
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            
            UpdateAnimator(grounded);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                if (distanceToPlayer < detectionRange) currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                HandleChase(distanceToPlayer);
                break;

            case EnemyState.Retreat:
                HandleRetreat(distanceToPlayer);
                break;
        }

        UpdateAnimator(grounded);
    }

    void HandleChase(float dist)
    {
        LookAtTarget(playerTarget.position);

        if (dist > attackRange)
        {
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            MoveInDirection(dir, chaseSpeed);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            rb.linearVelocity = Vector3.zero;
            StartCoroutine(AttackComboRoutine());
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleRetreat(float dist)
    {
        LookAtTarget(playerTarget.position);

        if (dist < 4.5f)
        {
            Vector3 dirAway = (transform.position - playerTarget.position).normalized;
            MoveInDirection(dirAway, retreatSpeed);
        }
        else
        {
            currentState = EnemyState.Chase;
        }
    }

    IEnumerator AttackComboRoutine()
    {
        canAction = false;
        currentState = EnemyState.Attack;

        yield return StartCoroutine(ExecuteSingleAttack(1));

        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (Random.value < comboChance && dist < attackRange + 1.5f)
        {
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(ExecuteSingleAttack(2));
        }

        lastAttackTime = Time.time;
        currentState = EnemyState.Retreat;
        canAction = true;
    }

    IEnumerator ExecuteSingleAttack(int index)
    {
        anim.SetInteger("ComboIndex", index);
        anim.SetTrigger("Attack");

        if (enemyWeapon != null) enemyWeapon.StartAttack();

        float timer = 0.25f;
        while (timer > 0)
        {
            Vector3 dashDir = (playerTarget.position - transform.position).normalized;
            rb.linearVelocity = new Vector3(dashDir.x * attackDashForce, rb.linearVelocity.y, dashDir.z * attackDashForce);
            timer -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        yield return new WaitForSeconds(0.4f);
        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    public void OnParried()
    {
        StopAllCoroutines();
        if (enemyWeapon != null) enemyWeapon.StopAttack();
        StartCoroutine(StaggerRoutine());
    }

    IEnumerator StaggerRoutine()
    {
        canAction = false;
        currentState = EnemyState.Stagger;
        rb.linearVelocity = Vector3.zero;
        
        if (anim != null) anim.SetTrigger(staggerTrigger);
        yield return new WaitForSeconds(staggerDuration);

        canAction = true;
        currentState = EnemyState.Chase;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (currentState != EnemyState.Stagger)
        {
            anim.SetTrigger(flinchTrigger);
        }
    }

    void MoveInDirection(Vector3 dir, float speed)
    {
        dir.y = 0;
        Vector3 vel = dir * speed;
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;
    }

    void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    bool CheckIfGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }

    void UpdateAirborneState()
    {
        if (anim == null) return;
        anim.SetBool(hashOnGround, false);
        anim.SetFloat(hashVertVel, -5.0f);
        anim.SetFloat(hashSpeed, 0f);
    }

    void UpdateAnimator(bool grounded)
    {
        if (anim == null) return;

        float speedVal = 0;
        if (currentState == EnemyState.Chase) speedVal = 1f;
        else if (currentState == EnemyState.Retreat) speedVal = -0.5f;

        anim.SetFloat(hashSpeed, speedVal, 0.1f, Time.fixedDeltaTime);
        anim.SetBool(hashIsStaggered, currentState == EnemyState.Stagger);
        anim.SetBool(hashOnGround, grounded);
        anim.SetFloat(hashVertVel, rb.linearVelocity.y);
    }
}