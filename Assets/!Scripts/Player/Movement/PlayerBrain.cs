using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    [Header("Core References")]
    public PlayerInput input;
    public PlayerMovement move;
    public PlayerDodgeSystem dodge;
    public LockOnSystem lockOn;
    public PlayerClimbing climbing;

    [Header("Global State")]
    public bool isGrounded;

    void Update()
    {
        // 1. Odczyt wejścia
        input.ReadInput();
        
        // 2. Aktualizacja stanu uziemienia
        isGrounded = Physics.CheckSphere(transform.position + move.groundCheckOffset, move.groundCheckDistance, move.groundLayer);

        // 3. Logika systemów (Ticki)
        lockOn.Tick();
        climbing.Tick();

        // 4. Zarządzanie Unikiem
        // Unik zablokowany, jeśli gracz się wspina lub jest w trakcie animacji startu wspinaczki
        if (!climbing.isClimbing && !climbing.isStartingClimb)
        {
            dodge.TickDodge(lockOn.IsLockedOn);
        }

        // 5. Skok i Animator
        // Wyłączone podczas wspinaczki i uniku, aby zapobiec konfliktom stanów
        if (!dodge.IsDodging && !climbing.isClimbing)
        {
            move.HandleJump(isGrounded);
            move.UpdateAnimator(isGrounded, lockOn.IsLockedOn, false);
        }
    }

    void FixedUpdate()
    {
        // PRIORYTETY FIZYKI:

        // Priorytet 1: Wspinaczka (Przejmuje kontrolę nad RB, wyłącza grawitację)
        if (climbing.isClimbing)
        {
            climbing.FixedTick();
        }
        // Priorytet 2: Ruch standardowy (Tylko jeśli nie trwa unik/roll)
        else if (!dodge.IsDodging)
        {
            move.ExecutePhysics(lockOn.IsLockedOn, lockOn.currentTarget);
        }
    }
}