using UnityEngine;

public class PlayerBrain : MonoBehaviour
{
    [Header("Core References")]
    public PlayerInput input;
    public PlayerMovement move;
    public PlayerDodgeSystem dodge;
    public LockOnSystem lockOn;
    public PlayerClimbing climbing;
    public Animator animator; // Dodana referencja do Animatora

    [Header("Global State")]
    public bool isGrounded;
    public bool isMeditating; // Nowy stan logiczny

    void Update()
    {
        // Pobieramy stan z animatora
        if (animator != null)
        {
            isMeditating = animator.GetBool("IsMeditating");
        }

        // 1. ZABLOKOWANE WEJŚCIE (Jeśli siedzi, nie czytamy inputu walki/ruchu)
        if (isMeditating)
        {
            // Opcjonalnie: zerujemy wartości w PlayerInput, aby postać nie "pamiętała" kierunku ruchu
            // input.ClearInput(); 
            return; // PRZERWANIU UPDATE - nic poniżej się nie wykona, gdy gracz siedzi
        }

        // --- RESZTA LOGIKI (wykona się tylko, gdy isMeditating == false) ---

        // 2. Odczyt wejścia
        input.ReadInput();
        
        // 3. Aktualizacja stanu uziemienia
        isGrounded = Physics.CheckSphere(transform.position + move.groundCheckOffset, move.groundCheckDistance, move.groundLayer);

        // 4. Logika systemów (Ticki)
        lockOn.Tick();
        climbing.Tick();

        // 5. Zarządzanie Unikiem
        if (!climbing.isClimbing && !climbing.isStartingClimb)
        {
            dodge.TickDodge(lockOn.IsLockedOn);
        }

        // 6. Skok i Animator
        if (!dodge.IsDodging && !climbing.isClimbing)
        {
            move.HandleJump(isGrounded);
            move.UpdateAnimator(isGrounded, lockOn.IsLockedOn, false);
        }
    }

    void FixedUpdate()
    {
        // Jeśli siedzi, nie wykonujemy fizyki ruchu
        if (isMeditating) return;

        // PRIORYTETY FIZYKI:

        // Priorytet 1: Wspinaczka
        if (climbing.isClimbing)
        {
            climbing.FixedTick();
        }
        // Priorytet 2: Ruch standardowy
        else if (!dodge.IsDodging)
        {
            move.ExecutePhysics(lockOn.IsLockedOn, lockOn.currentTarget);
        }
    }
}