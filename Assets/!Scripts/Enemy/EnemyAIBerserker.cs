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

    private Animator anim;
    private Rigidbody rb;
    private float lastAttackTime;
    private bool canAction = true;
    private bool isDead = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (enemyWeapon != null) enemyWeapon.StopAttack();
    }

    void Update()
    {
        if (isDead || !canAction || playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer < detectionRange) currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                HandleChase(distanceToPlayer);
                break;

            case EnemyState.Retreat:
                HandleRetreat(distanceToPlayer);
                break;

            case EnemyState.Attack:
                break;

            case EnemyState.Stagger:
                break;
        }

        UpdateAnimator();
    }

    void HandleChase(float dist)
    {
        LookAtTarget(playerTarget.position);

        if (dist > attackRange)
        {
            MoveInDirection((playerTarget.position - transform.position).normalized, chaseSpeed);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackComboRoutine());
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

        Vector3 dashDir = (playerTarget.position - transform.position).normalized;
        float timer = 0.2f;
        
        if (enemyWeapon != null) enemyWeapon.StartAttack();

        while (timer > 0)
        {
            MoveInDirection(dashDir, attackDashForce);
            timer -= Time.deltaTime;
            yield return null;
        }

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
        
        if (anim != null) anim.SetTrigger(staggerTrigger);

        yield return new WaitForSeconds(staggerDuration);

        canAction = true;
        currentState = EnemyState.Chase;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        if (currentState == EnemyState.Stagger)
        {
            Debug.Log("Berserker damaged while staggered!");
        }
        else
        {
            anim.SetTrigger(flinchTrigger);
        }
    }

    void MoveInDirection(Vector3 dir, float speed)
    {
        dir.y = 0;
        transform.position += dir * speed * Time.deltaTime;
    }

    void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimator()
    {
        if (anim == null) return;

        float speedVal = 0;
        if (currentState == EnemyState.Chase) speedVal = 1f;
        else if (currentState == EnemyState.Retreat) speedVal = -0.5f;

        anim.SetFloat("speed", speedVal, 0.1f, Time.deltaTime);
        anim.SetBool("IsStaggered", currentState == EnemyState.Stagger);
    }
}