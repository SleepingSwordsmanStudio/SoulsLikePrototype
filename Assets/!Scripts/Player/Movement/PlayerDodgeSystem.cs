using UnityEngine;
using System.Collections;

public class PlayerDodgeSystem : MonoBehaviour
{
    public PlayerMovement move;
    public PlayerInput input;
    public PlayerHealth health;
    public Rigidbody rb;

    [Header("Roll Settings")]
    public float rollForce = 20f;
    public float rollDuration = 0.6f;
    public float rollStartupDelay = 0.1f;
    public float iFrameDuration = 0.45f;
    public float rollCooldown = 0.3f;

    [Header("Dodge Settings")]
    public float dodgeForce = 12f;
    public float dodgeDuration = 0.4f;
    public float doubleTapTime = 0.22f;

    public bool IsDodging { get; private set; }
    private bool canDodge = true;
    private float lastTapTime;
    private Coroutine pendingDodgeCoroutine;

    public void TickDodge(bool isLockedOn)
    {
        if (!canDodge || IsDodging || !move.CanMove) return;

        if (input.IsDodgePressed)
        {
            float timeSinceLastTap = Time.time - lastTapTime;
            if (timeSinceLastTap <= doubleTapTime)
            {
                if (pendingDodgeCoroutine != null) StopCoroutine(pendingDodgeCoroutine);
                StartCoroutine(RollRoutine());
                lastTapTime = 0f;
            }
            else
            {
                lastTapTime = Time.time;
                if (isLockedOn)
                {
                    if (pendingDodgeCoroutine != null) StopCoroutine(pendingDodgeCoroutine);
                    pendingDodgeCoroutine = StartCoroutine(WaitAndCheckDodge());
                }
                else StartCoroutine(RollRoutine());
            }
        }
    }

    IEnumerator WaitAndCheckDodge()
    {
        yield return new WaitForSeconds(doubleTapTime);
        StartCoroutine(DodgeRoutine());
        pendingDodgeCoroutine = null;
    }

    IEnumerator RollRoutine()
    {
        IsDodging = true; canDodge = false;
        Vector3 rollDir = input.GetMovementDirection(move.cameraTransform);
        if (rollDir.sqrMagnitude < 0.01f) rollDir = transform.forward;

        transform.rotation = Quaternion.LookRotation(rollDir);
        rb.linearVelocity = Vector3.zero;
        move.animator.SetTrigger("Roll");
        if (health) health.isInvulnerable = true;

        float timer = 0f;
        while (timer < rollDuration)
        {
            timer += Time.deltaTime;
            if (timer > iFrameDuration && health) health.isInvulnerable = false;

            float speed = (timer < rollStartupDelay) ? rollForce * 0.5f : Mathf.Lerp(rollForce, 0f, (timer - rollStartupDelay) / (rollDuration - rollStartupDelay));
            rb.linearVelocity = new Vector3(rollDir.x * speed, rb.linearVelocity.y, rollDir.z * speed);
            yield return null;
        }

        FinishAction(rollCooldown);
    }

    IEnumerator DodgeRoutine()
    {
        IsDodging = true; canDodge = false;
        Vector3 moveDir = input.GetMovementDirection(move.cameraTransform);
        Vector3 forceDir = (moveDir.sqrMagnitude < 0.01f) ? -transform.forward : moveDir;

        Vector3 localDir = transform.InverseTransformDirection(forceDir);
        move.animator.SetFloat("VelocityX", localDir.x);
        move.animator.SetFloat("VelocityZ", localDir.z);
        move.animator.SetTrigger("Dodge");

        if (health) health.isInvulnerable = true;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(forceDir * dodgeForce, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dodgeDuration);
        FinishAction(0.2f);
    }

    void FinishAction(float cooldown)
    {
        if (health) health.isInvulnerable = false;
        IsDodging = false;
        StartCoroutine(ResetCooldown(cooldown));
    }

    IEnumerator ResetCooldown(float time) { yield return new WaitForSeconds(time); canDodge = true; }
}