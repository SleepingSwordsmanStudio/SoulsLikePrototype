using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerClimbing : MonoBehaviour
{
    [Header("Referencje")]
    public PlayerBrain brain;
    public PlayerInput input;
    public PlayerMovement moveScript;
    public LockOnSystem lockSystem; 
    public Rigidbody rb;
    public Animator animator;
    public CapsuleCollider playerCollider;

    [Header("Ustawienia Wykrywania")]
    public LayerMask climbableLayer;      
    public float detectionDistance = 1.2f; 
    public float wallOffset = 0.35f;       
    public float ledgeCheckHeight = 1.8f; 
    public float snapSpeed = 10f; 

    [Header("Ustawienia Collidera")]
    public float climbRadius = 0.2f; 
    private float normalRadius;

    [Header("Ustawienia Ruchu")]
    public float climbSpeed = 3f;
    public float wallJumpForce = 12f;      // Siła skoku wzdłuż ściany (góra/boki)
    public float wallJumpBackForce = 15f;  // NOWE: Siła odskoku od ściany (S + Spacja)
    public float jumpChargeTime = 0.2f; 
    public float jumpLockDuration = 0.5f; 
    public float startClimbLockDuration = 0.4f;

    [Header("Stan (Podgląd)")]
    public bool isClimbing = false;
    public bool isJumping = false; 
    public bool isStartingClimb = false;
    public bool isChargingJump = false; 
    
    private RaycastHit wallHit;
    private float jumpCooldown = 0f;
    private float jumpLockTimer = 0f;
    private float startClimbTimer = 0f;
    private float chargeTimer = 0f; 
    
    private Vector3 pendingJumpDir; 
    private bool pendingShouldRelease; 

    private readonly int hashIsClimbing = Animator.StringToHash("isClimbing");
    private readonly int hashClimbTrigger = Animator.StringToHash("climbTrigger");
    private readonly int hashVelX = Animator.StringToHash("VelocityX");
    private readonly int hashVelZ = Animator.StringToHash("VelocityZ");
    private readonly int hashJumpDir = Animator.StringToHash("JumpDirection");
    private readonly int hashClimbJumpTrigger = Animator.StringToHash("climbJumpTrigger");

    private void Start()
    {
        if (playerCollider != null)
            normalRadius = playerCollider.radius;
    }

    public void Tick()
    {
        HandleClimbToggle();

        if (isClimbing && input.IsJumpPressed && !isJumping && !isStartingClimb && !isChargingJump)
        {
            PrepareWallJump();
        }

        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;
        
        if (jumpLockTimer > 0) jumpLockTimer -= Time.deltaTime;
        else if (isJumping) isJumping = false;

        if (startClimbTimer > 0) startClimbTimer -= Time.deltaTime;
        else isStartingClimb = false;

        if (isChargingJump)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0)
            {
                ExecuteWallJump();
            }
        }
    }

    public void FixedTick()
    {
        if (brain.dodge.IsDodging) 
        {
            if (isClimbing) StopClimbing();
            return; 
        }

        Vector3 rayStart = transform.position + Vector3.up * 1f;

        if (isClimbing)
        {
            if (isJumping || isChargingJump) 
            {
                if (isChargingJump) rb.linearVelocity = Vector3.zero;
                return;
            }

            if (Physics.Raycast(rayStart, transform.forward, out wallHit, detectionDistance + 0.5f, climbableLayer))
            {
                SnapToWall();

                if (!isStartingClimb) 
                {
                    RotateTowardsWall();
                    HandleClimbingMovement();
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                    RotateTowardsWall();
                }
            }
            else if (jumpCooldown <= 0) 
            {
                StopClimbing();
            }
        }
        else
        {
            if (!brain.isGrounded && jumpCooldown <= 0)
            {
                if (Physics.Raycast(rayStart, transform.forward, out wallHit, detectionDistance, climbableLayer))
                {
                    StartClimbing();
                }
            }
        }
    }

    private void SnapToWall()
    {
        Vector3 targetPos = wallHit.point + wallHit.normal * wallOffset;
        Vector3 newPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        transform.position = Vector3.Lerp(transform.position, newPos, snapSpeed * Time.fixedDeltaTime);
    }

    private void RotateTowardsWall()
    {
        Vector3 faceWallDir = -wallHit.normal;
        faceWallDir.y = 0; 
        if (faceWallDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(faceWallDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.fixedDeltaTime);
        }
    }

    private void HandleClimbToggle()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isClimbing)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 1f, transform.forward, out wallHit, detectionDistance, climbableLayer))
                {
                    StartClimbing();
                }
            }
            else StopClimbing();
        }
        if (isClimbing && Input.GetKeyDown(KeyCode.C)) StopClimbing();
    }

    private void StartClimbing()
    {
        if (lockSystem != null && lockSystem.IsLockedOn) lockSystem.Unlock();
        
        isClimbing = true;
        isJumping = false;
        isChargingJump = false;
        isStartingClimb = true;
        startClimbTimer = startClimbLockDuration;
        
        moveScript.CanMove = false; 
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        if (playerCollider != null)
            playerCollider.radius = climbRadius;

        Vector3 faceWallDir = -wallHit.normal;
        faceWallDir.y = 0; 
        transform.rotation = Quaternion.LookRotation(faceWallDir);

        Vector3 targetPos = wallHit.point + wallHit.normal * wallOffset;
        transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        if (animator)
        {
            animator.SetBool(hashIsClimbing, true);
            animator.SetTrigger(hashClimbTrigger);
            animator.SetFloat(hashVelX, 0);
            animator.SetFloat(hashVelZ, 0);
        }
    }

    public void StopClimbing()
    {
        isClimbing = false;
        isJumping = false;
        isChargingJump = false;
        isStartingClimb = false;
        moveScript.CanMove = true; 
        rb.useGravity = true;

        if (playerCollider != null)
            playerCollider.radius = normalRadius;

        if (animator)
        {
            animator.SetBool(hashIsClimbing, false);
            animator.ResetTrigger(hashClimbTrigger);
        }
    }

    private void HandleClimbingMovement()
    {
        float h = input.Horizontal;
        float v = input.Vertical;
        bool canMoveUp = Physics.Raycast(transform.position + Vector3.up * ledgeCheckHeight, transform.forward, detectionDistance + 0.2f, climbableLayer);
        if (v > 0.1f && !canMoveUp) v = 0;

        Vector3 moveDir = (transform.up * v + transform.right * h).normalized;
        
        if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f) rb.linearVelocity = Vector3.zero;
        else rb.linearVelocity = moveDir * climbSpeed;

        if (animator)
        {
            animator.SetFloat(hashVelZ, v, 0.1f, Time.fixedDeltaTime);
            animator.SetFloat(hashVelX, h, 0.1f, Time.fixedDeltaTime);
        }

        if (v < -0.1f && brain.isGrounded) StopClimbing();
    }

    private void PrepareWallJump()
    {
        float h = input.Horizontal;
        float v = input.Vertical;

        if (animator)
        {
            Vector2 inputVec = new Vector2(h, v);
            float inputAngle = Vector2.SignedAngle(Vector2.up, inputVec);
            float animVal = (inputVec.magnitude < 0.2f) ? -3.0f : 
                            (inputAngle > -45f && inputAngle <= 45f) ? 0f :
                            (inputAngle > 45f && inputAngle <= 135f) ? 1f :
                            (inputAngle < -45f && inputAngle >= -135f) ? -1f : -2f;
            
            animator.SetFloat(hashJumpDir, animVal);
            animator.SetTrigger(hashClimbJumpTrigger);
        }

        pendingShouldRelease = false;

        if (v > 0.1f) 
        {
            pendingJumpDir = transform.up;
        }
        else if (Mathf.Abs(h) > 0.1f && v >= -0.1f)
        {
            pendingJumpDir = (transform.right * h + transform.up * 0.4f).normalized;
        }
        else 
        {
            pendingJumpDir = Vector3.zero; 
            pendingShouldRelease = true; 
        }

        isChargingJump = true;
        chargeTimer = jumpChargeTime;
        rb.linearVelocity = Vector3.zero;
    }

    private void ExecuteWallJump()
    {
        isChargingJump = false;
        isJumping = true;
        jumpLockTimer = jumpLockDuration;
        jumpCooldown = 0.5f;

        Vector3 finalJumpForce;
        float appliedForce;

        if (pendingShouldRelease)
        {
            // ODSKOK W TYŁ: Używamy nowej zmiennej wallJumpBackForce
            finalJumpForce = (wallHit.normal * 1.0f + Vector3.up * 1.2f).normalized;
            appliedForce = wallJumpBackForce;
            StopClimbing();
        }
        else
        {
            // SKOK PO ŚCIANIE: Używamy standardowej zmiennej wallJumpForce
            finalJumpForce = pendingJumpDir;
            appliedForce = wallJumpForce;
        }

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(finalJumpForce * appliedForce, ForceMode.VelocityChange);

        if (pendingShouldRelease)
        {
            Vector3 lookDir = finalJumpForce;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        pendingShouldRelease = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 1f, transform.forward * detectionDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * ledgeCheckHeight, transform.forward * (detectionDistance + 0.2f));
    }
}