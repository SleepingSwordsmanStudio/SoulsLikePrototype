using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCombat : MonoBehaviour
{
    public Animator animator;
    public string layerNameAtack = "Atack";
    public string layerNameDefense = "Defense";
    
    [Header("References")]
    public PlayerClimbing climbing;
    public LockOnSystem lockOn; 
    public Rigidbody rb;

    [Header("Weapon Visuals")]
    public GameObject weaponInHand; 
    public GameObject weaponOnBack; 

    [Header("Weapon Settings")]
    public WeaponHitbox weapon; 
    public float attackDuration = 0.6f;
    public float weaponSheathDelay = 2.0f; 

    [Header("Combo Settings")]
    public float comboResetTime = 1.1f;
    public float layerFadeSpeed = 5f; 

    [Header("Attack Movement (Step In)")]
    public float attackStepForce = 5f;     
    public float attackStepDuration = 0.15f; 
    private float stepTimer = 0f;

    [Header("Soft Lock-on Settings")]
    public LayerMask enemyLayer;           
    public float softLockRadius = 5f;      
    public float softLockAngle = 60f;      
    public float autoRotationSpeed = 720f; 
    private Transform softTarget;

    [Header("Block & Parry Settings")]
    public string blockBoolParameter = "isBlocking";
    public float parryWindow = 0.25f; 
    public string parryTrigger = "ParrySuccess";
    private float parryTimer = 0f;
    
    private int comboIndex = 0;
    private float comboTimer = 0f;
    private float sheathTimer = 0f; 
    private int attackLayerIndex;
    private int defenseLayerIndex;
    private bool inputBuffer = false; 
    private bool isBlocking = false;

    public bool IsBlocking => isBlocking;

    public bool IsAttacking 
    {
        get 
        {
            if (attackLayerIndex == -1) return false;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(attackLayerIndex);
            return stateInfo.IsTag("Attack");
        }
    }

    void Start()
    {
        attackLayerIndex = animator.GetLayerIndex(layerNameAtack);
        defenseLayerIndex = animator.GetLayerIndex(layerNameDefense);

        if (attackLayerIndex == -1) Debug.LogError("Nie znaleziono warstwy Ataku: " + layerNameAtack);
        if (defenseLayerIndex == -1) Debug.LogError("Nie znaleziono warstwy Obrony: " + layerNameDefense);
        
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (weapon != null) weapon.StopAttack();
        UpdateWeaponVisibility(false);
    }

    void Update()
    {
        if (attackLayerIndex == -1) return;

        if (climbing != null && climbing.isClimbing)
        {
            ForceStopCombat();
            UpdateWeaponVisibility(false);
            return;
        }

        if (parryTimer > 0) parryTimer -= Time.deltaTime;

        HandleInput();    
        HandleBlock();    
        ExecuteAttack();  
        ResetCombo();
        HandleLayerWeights();

        bool isLockedOn = (lockOn != null && lockOn.IsLockedOn);
        if (sheathTimer > 0) sheathTimer -= Time.deltaTime;

        bool shouldShowWeapon = (comboIndex > 0 || comboTimer > 0 || isLockedOn || sheathTimer > 0 || isBlocking || IsAttacking);
        UpdateWeaponVisibility(shouldShowWeapon);
    }

    void FixedUpdate()
    {
        if (IsAttacking && softTarget != null)
        {
            Vector3 dirToEnemy = (softTarget.position - transform.position).normalized;
            dirToEnemy.y = 0; 

            if (dirToEnemy != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToEnemy);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, autoRotationSpeed * Time.fixedDeltaTime);
            }
        }

        if (stepTimer > 0)
        {
            stepTimer -= Time.fixedDeltaTime;
            Vector3 moveDir = transform.forward * attackStepForce;
            rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && !isBlocking)
        {
            inputBuffer = true;
        }
    }

    void HandleBlock()
    {
        bool mouseHold = Input.GetMouseButton(1);
        bool mouseDown = Input.GetMouseButtonDown(1);

        if (mouseDown && !IsAttacking && !isBlocking)
        {
            parryTimer = parryWindow;
        }

        if (mouseHold && !IsAttacking)
        {
            if (!isBlocking)
            {
                isBlocking = true;
                animator.SetBool(blockBoolParameter, true);
            }
            sheathTimer = weaponSheathDelay;
        }
        else
        {
            if (isBlocking && !mouseHold)
            {
                isBlocking = false;
                animator.SetBool(blockBoolParameter, false);
                parryTimer = 0;
            }
        }
    }

    void ExecuteAttack()
    {
        if (inputBuffer)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(attackLayerIndex);
            bool canChain = stateInfo.IsTag("Attack") && stateInfo.normalizedTime >= 0.5f;
            bool isIdle = stateInfo.IsName("Empty") || stateInfo.normalizedTime >= 0.95f;

            if ((isIdle || canChain) && !animator.IsInTransition(attackLayerIndex))
            {
                softTarget = FindBestSoftTarget();

                comboIndex++;
                if (comboIndex > 3) comboIndex = 1;

                animator.SetInteger("ComboIndex", comboIndex);
                animator.SetTrigger("Attack");

                stepTimer = attackStepDuration;

                if (weapon != null)
                {
                    CancelInvoke("DisableHitbox"); 
                    weapon.StartAttack();
                    Invoke("DisableHitbox", attackDuration);
                }

                comboTimer = comboResetTime;
                sheathTimer = weaponSheathDelay;
                inputBuffer = false; 
            }
        }
    }

    void HandleLayerWeights()
    {
        float targetAtk = (comboIndex > 0 || IsAttacking) ? 1f : 0f;
        float curAtk = animator.GetLayerWeight(attackLayerIndex);
        animator.SetLayerWeight(attackLayerIndex, Mathf.MoveTowards(curAtk, targetAtk, Time.deltaTime * layerFadeSpeed));

        float targetDef = isBlocking ? 1f : 0f;
        float curDef = animator.GetLayerWeight(defenseLayerIndex);
        animator.SetLayerWeight(defenseLayerIndex, Mathf.MoveTowards(curDef, targetDef, Time.deltaTime * layerFadeSpeed));
    }

    private Transform FindBestSoftTarget()
    {
        if (lockOn != null && lockOn.IsLockedOn) return null;

        Collider[] potentialEnemies = Physics.OverlapSphere(transform.position, softLockRadius, enemyLayer);
        Transform bestTarget = null;
        float closestAngle = softLockAngle;

        foreach (var col in potentialEnemies)
        {
            if (!col.CompareTag("Enemy")) continue;

            Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
            float angleToEnemy = Vector3.Angle(transform.forward, dirToEnemy);

            if (angleToEnemy < closestAngle)
            {
                closestAngle = angleToEnemy;
                bestTarget = col.transform;
            }
        }
        return bestTarget;
    }

    public void TakeDamage(int dmg)
    {
        if (parryTimer > 0)
        {
            animator.SetTrigger(parryTrigger);
            parryTimer = 0; 
            StunEnemyInFront();
            return; 
        }

        if (isBlocking)
        {
            animator.SetTrigger("BlockImpact");
            return;
        }

        animator.SetTrigger("Hit"); 
    }

    private void StunEnemyInFront()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, 2f, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            enemy.SendMessage("OnParried", SendMessageOptions.DontRequireReceiver);
            if (weapon != null) weapon.StartCoroutine("DoHitStop"); 
        }
    }

    public void ForceStopCombat()
    {
        inputBuffer = false;
        isBlocking = false;
        stepTimer = 0;
        softTarget = null;
        animator.SetBool(blockBoolParameter, false);
        comboIndex = 0;
        comboTimer = 0;
        sheathTimer = 0;
        animator.SetInteger("ComboIndex", 0);
        animator.SetLayerWeight(attackLayerIndex, 0);
        animator.SetLayerWeight(defenseLayerIndex, 0);
        DisableHitbox();
    }

    void DisableHitbox() => weapon?.StopAttack();

    void ResetCombo()
    {
        if (comboTimer > 0) comboTimer -= Time.deltaTime;
        else if (comboIndex != 0)
        {
            comboIndex = 0;
            animator.SetInteger("ComboIndex", 0);
            inputBuffer = false;
            softTarget = null;
        }
    }

    private void UpdateWeaponVisibility(bool showInHand)
    {
        if (weaponInHand != null && weaponInHand.activeSelf != showInHand) 
            weaponInHand.SetActive(showInHand);
        if (weaponOnBack != null && weaponOnBack.activeSelf != !showInHand) 
            weaponOnBack.SetActive(!showInHand);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, softLockRadius);
        Vector3 leftBoundary = Quaternion.Euler(0, -softLockAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, softLockAngle, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * softLockRadius);
        Gizmos.DrawRay(transform.position, rightBoundary * softLockRadius);
    }
}