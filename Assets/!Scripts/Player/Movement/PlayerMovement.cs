using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Required References")]
    public PlayerInput input; 
    public Rigidbody rb;
    public Animator animator;
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float acceleration = 12f;
    public float rotationSpeed = 10f;

    [Header("Jumping & Ground Check")]
    public float jumpHeight = 2.0f;
    public LayerMask groundLayer;           // DODANE: Publiczne dla Brain i Climbing
    public float groundCheckDistance = 0.3f; // DODANE: Publiczne dla Brain
    public Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0); // DODANE: Publiczne dla Brain

    [Header("State")]
    public bool CanMove = true;

    // Hashes dla wydajności animatora
    private readonly int hashVelocityX = Animator.StringToHash("VelocityX");
    private readonly int hashVelocityZ = Animator.StringToHash("VelocityZ");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");
    private readonly int hashIsLockedOn = Animator.StringToHash("isLockedOn");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");

    /// <summary>
    /// Wywoływane przez PlayerBrain w FixedUpdate.
    /// </summary>
    public void ExecutePhysics(bool isLockedOn, Transform target)
    {
        // Jeśli nie możemy się ruszać (np. wspinaczka, stun), wyhamuj postać
        if (!CanMove) {
            Vector3 stopVel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(stopVel.x * 0.9f, stopVel.y, stopVel.z * 0.9f);
            return;
        }

        Vector3 moveDir = input.GetMovementDirection(cameraTransform);
        float moveMag = Mathf.Clamp01(new Vector2(input.Horizontal, input.Vertical).magnitude);
        
        // Obliczanie prędkości docelowej
        float targetSpeed = (input.IsSprintPressed && moveMag > 0.1f) ? sprintSpeed : walkSpeed;
        if (moveMag < 0.1f) targetSpeed = 0;

        HandleRotation(isLockedOn, target, moveDir, moveMag);
        HandleVelocity(moveDir, targetSpeed);
    }

private void HandleRotation(bool isLockedOn, Transform target, Vector3 moveDir, float moveMag)
{
    if (!isLockedOn)
    {
        // Swobodny ruch: obracaj w stronę biegu
        if (moveMag > 0.1f && moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }
    else if (target != null)
    {
        // LOCK-ON: Tutaj była kolizja! 
        // Zamiast robić rotację tutaj, sprawdzamy czy mamy dostęp do LockOnSystem
        // i wywołujemy jego metodę, która posiada Deadzone.
        
        LockOnSystem lo = GetComponent<LockOnSystem>();
        if (lo != null)
        {
            lo.RotatePlayerTowardsTarget(); // Ta metoda ma w sobie Deadzone!
        }
        else
        {
            // Failsafe: jeśli nie ma skryptu LockOnSystem na tym samym obiekcie
            Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).normalized;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}

    private void HandleVelocity(Vector3 moveDir, float targetSpeed)
    {
        Vector3 targetVelocity = moveDir * targetSpeed;
        Vector3 currentVel = rb.linearVelocity;
        
        // Obliczamy zmianę prędkości tylko w osiach X i Z
        Vector3 velocityChange = (targetVelocity - new Vector3(currentVel.x, 0, currentVel.z));
        
        // Ograniczamy siłę przyspieszenia (żeby postać nie "teleportowała" się do max prędkości)
        velocityChange.x = Mathf.Clamp(velocityChange.x, -acceleration, acceleration);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -acceleration, acceleration);
        velocityChange.y = 0;
        
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Wywoływane przez PlayerBrain w Update.
    /// </summary>
    public void HandleJump(bool isGrounded)
    {
        if (isGrounded && input.IsJumpPressed)
        {
            // Resetujemy prędkość pionową przed skokiem dla równej siły
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Wywoływane przez PlayerBrain w Update.
    /// </summary>
    public void UpdateAnimator(bool isGrounded, bool isLockedOn, bool isDodging)
{
    if (animator == null) return;

    animator.SetBool(hashOnGround, isGrounded);
    animator.SetBool(hashIsLockedOn, isLockedOn);
    animator.SetFloat(hashVertVel, rb.linearVelocity.y);

    if (!isDodging)
    {
        if (!isLockedOn)
        {
            // --- TRYB EWDEN RING / SWOBODNY ---
            // Obliczamy ogólną prędkość poziomą (bez osi Y)
            float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
            
            // W trybie swobodnym postać zawsze porusza się "do przodu" (VelocityZ) względem własnego modelu
            // bo rotacja (HandleRotation) dba o to, by model patrzył tam, gdzie idziemy.
            animator.SetFloat(hashVelocityZ, horizontalSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat(hashVelocityX, 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            // --- TRYB WALKI (LOCK-ON) ---
            // Tutaj postać musi umieć chodzić tyłem i bokiem (strafe)
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            animator.SetFloat(hashVelocityX, localVel.x, 0.1f, Time.deltaTime);
            animator.SetFloat(hashVelocityZ, localVel.z, 0.1f, Time.deltaTime);
        }
    }
}
}