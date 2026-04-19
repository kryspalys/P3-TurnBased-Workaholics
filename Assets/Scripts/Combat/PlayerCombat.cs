using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Manages the player's (Grandma's) specific combat actions, animations, and turn limit enforcement.
/// </summary>
/// <remarks>
/// <para>Damage is not applied directly via button clicks. Instead, clicks trigger animations, and Animation Events call execution methods.</para>
/// </remarks>
public class PlayerCombat : MonoBehaviour
{
    [Header("Dependencies")]
    /// <summary>Reference to the global turn authority.</summary>
    [SerializeField] private TurnManager turnManager;
    /// <summary>Reference to the antagonist's health component.</summary>
    [SerializeField] private Health enemyHealth;
    /// <summary>Reference to the protagonist's health component.</summary>
    [SerializeField] private Health playerHealth;

    [Header("Stats")]
    /// <summary>Raw damage dealt by the basic attack.</summary>
    [SerializeField] private float caneDamage = 15f;
    /// <summary>Raw hit points restored by the healing action.</summary>
    [SerializeField] private float cookieHealAmount = 25f;
    /// <summary>Raw damage dealt by the ultimate attack.</summary>
    [SerializeField] private float purseSlamDamage = 40f;

    /// <summary>The maximum capacity of the ultimate meter.</summary>
    [SerializeField] private int maxGrandmaMeter = 3;
    /// <summary>The current charge level of the ultimate meter.</summary>
    private int currentGrandmaMeter = 0;

    /// <summary>Broadcasts changes to the ultimate meter for UI updates.</summary>
    public UnityEvent<int, int> OnMeterUpdated;

    /// <summary>The animator component driving visual feedback.</summary>
    private Animator characterAnimator;

    /// <summary>
    /// Caches essential components during initialization.
    /// </summary>
    /// <exception cref="UnityEngine.MissingComponentException">Thrown if no Animator is attached to the GameObject.</exception>
    private void Awake()
    {
        if (!TryGetComponent<Animator>(out characterAnimator))
        {
            throw new MissingComponentException("Animator missing on Player object.");
        }
    }

    /// <summary>
    /// Initiates the basic melee attack sequence.
    /// </summary>
    public void Action_CaneWhack()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        UpdateGrandmaMeter(1);

        // The Fallback Logic
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Attack");
            // We wait for the Animation Event to call ExecuteMeleeDamage()
        }
        else
        {
            ExecuteMeleeDamage(); // No animator? Just hit them instantly!
        }
    }

    /// <summary>
    /// Initiates the defensive block sequence and immediately ends the turn.
    /// </summary>
    public void Action_KnittingShield()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        characterAnimator.SetTrigger("Defend");
        playerHealth.SetDefending(true);
        EndTurn();
    }

    /// <summary>
    /// Initiates the healing sequence and immediately ends the turn.
    /// </summary>
    public void Action_BakeCookies()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        characterAnimator.SetTrigger("Heal");
        playerHealth.Heal(cookieHealAmount);
        EndTurn();
    }

    /// <summary>
    /// Initiates the ultimate attack sequence if the meter is fully charged.
    /// </summary>
    public void Action_PurseSlam()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || currentGrandmaMeter < maxGrandmaMeter) return;

        characterAnimator.SetTrigger("Special");
        currentGrandmaMeter = 0;
        OnMeterUpdated?.Invoke(currentGrandmaMeter, maxGrandmaMeter);
    }

    /// <summary>
    /// Executes the damage application for a basic attack.
    /// </summary>
    /// <remarks>
    /// <c>ExecuteMeleeDamage</c> MUST be triggered via a Unity Animation Event on the specific impact frame of the animation clip.
    /// </remarks>
    public void ExecuteMeleeDamage()
    {
        enemyHealth.TakeDamage(caneDamage);
        EndTurn();
    }

    /// <summary>
    /// Executes the damage application for the ultimate attack.
    /// </summary>
    public void ExecuteSpecialDamage()
    {
        enemyHealth.TakeDamage(purseSlamDamage);
        EndTurn();
    }

    /// <summary>
    /// Modifies the ultimate meter and broadcasts the change.
    /// </summary>
    /// <param name="amount">The integer amount to add to the meter.</param>
    private void UpdateGrandmaMeter(int amount)
    {
        currentGrandmaMeter += amount;
        currentGrandmaMeter = Mathf.Clamp(currentGrandmaMeter, 0, maxGrandmaMeter);
        OnMeterUpdated?.Invoke(currentGrandmaMeter, maxGrandmaMeter);
    }

    /// <summary>
    /// Concludes the player's action phase and passes control to the enemy.
    /// </summary>
    private void EndTurn()
    {
        turnManager.SwitchTurn(TurnState.EnemyTurn);
    }
}