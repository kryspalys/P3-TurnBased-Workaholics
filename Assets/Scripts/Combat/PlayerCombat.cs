using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Defines standard operations for player-controlled entities in the combat system.
/// </summary>
public interface IPlayerCombat
{
    /// <summary>
    /// Executes the primary melee attack logic.
    /// </summary>
    void Action_CaneWhack();
}

/// <summary>
/// Manages the player's (Grandma's) specific combat actions, animations, and turn limit enforcement.
/// </summary>
/// <remarks>
/// <para>This class acts as the bridge between player UI inputs and the underlying state machine.</para>
/// <list type="bullet">
/// <item><term>Animation Driven</term><description>Damage is applied via Animation Events when an animator is present.</description></item>
/// <item><term>Fallback Support</term><description>If no animator is found, calculations execute instantly.</description></item>
/// </list>
/// <example>
/// <code>
/// // Example of UI Event triggering the sequence:
/// myPlayerCombat.Action_CaneWhack();
/// </code>
/// </example>
/// <include file='ExternalDocs.xml' path='docs/members[@name="PlayerCombat"]/PlayerCombat/*'/>
/// </remarks>
/// <seealso cref="TurnManager"/>
public class PlayerCombat : MonoBehaviour, IPlayerCombat
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

    /// <summary>Tracks hit points to determine if damage was taken (triggering Hurt animation).</summary>
    private float previousHealth;

    /// <summary>Broadcasts changes to the ultimate meter for UI updates.</summary>
    public UnityEvent<int, int> OnMeterUpdated;

    /// <summary>The animator component driving visual feedback.</summary>
    private Animator characterAnimator;

    /// <summary>
    /// Exposes the current meter charge.
    /// </summary>
    /// <value>Returns an integer representing the <c>currentGrandmaMeter</c> charge.</value>
    public int CurrentMeter => currentGrandmaMeter;

    /// <summary>
    /// Caches essential components and sets initial state tracking during initialization.
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
    /// Subscribes to the health component to listen for damage and death events.
    /// </summary>
    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            playerHealth.OnDeath.AddListener(HandleDeath);
        }
    }

    /// <summary>
    /// Unsubscribes from events to prevent memory leaks when disabled.
    /// </summary>
    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            playerHealth.OnDeath.RemoveListener(HandleDeath);
        }
    }

    /// <inheritdoc/>
    public void Action_CaneWhack()
    {
        // FIX: Replaced GetCurrentTurn() with the CurrentTurn property
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        UpdateGrandmaMeter(1);

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("GrannyAttack");
        }
        else
        {
            ExecuteMeleeDamage();
        }
    }

    /// <summary>
    /// Initiates the defensive block sequence and immediately ends the turn.
    /// </summary>
    public void Action_KnittingShield()
    {
        // FIX: Replaced GetCurrentTurn() with the CurrentTurn property
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyBlock");
        playerHealth.SetDefending(true);
        EndTurn();
    }

    /// <summary>
    /// Initiates the healing sequence and immediately ends the turn.
    /// </summary>
    public void Action_BakeCookies()
    {
        // FIX: Replaced GetCurrentTurn() with the CurrentTurn property
        if (turnManager.CurrentTurn != TurnState.PlayerTurn) return;

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyHeal");
        playerHealth.Heal(cookieHealAmount);
        EndTurn();
    }

    /// <summary>
    /// Initiates the ultimate attack sequence if the meter is fully charged.
    /// </summary>
    public void Action_PurseSlam()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || currentGrandmaMeter < maxGrandmaMeter) return;

        currentGrandmaMeter = 0;
        OnMeterUpdated?.Invoke(currentGrandmaMeter, maxGrandmaMeter);

        // The Fallback Logic
        if (characterAnimator != null)
        {
            // Changed from GrannyAttack to the new unique special trigger
            characterAnimator.SetTrigger("GrannySpecial");
        }
        else
        {
            ExecuteSpecialDamage(); // No animator? Just hit them instantly!
        }
    }

    /// <summary>
    /// Evaluates if the player took damage to trigger the appropriate reaction.
    /// </summary>
    /// <param name="current">The current hit points broadcasted by the health script.</param>
    /// <param name="max">The maximum hit points broadcasted by the health script.</param>
    private void HandleHealthChanged(float current, float max)
    {
        // If current health is lower than it was previously, we took damage.
        if (current < previousHealth && characterAnimator != null)
        {
            characterAnimator.SetTrigger("GrannyHurt");
        }
        previousHealth = current;
    }

    /// <summary>
    /// Triggers the terminal animation sequence upon health reaching zero.
    /// </summary>
    private void HandleDeath()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyDeath");
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
        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>
    /// Executes the damage application for the ultimate attack.
    /// </summary>
    public void ExecuteSpecialDamage()
    {
        enemyHealth.TakeDamage(purseSlamDamage);
        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>
    /// Modifies the ultimate meter and broadcasts the change.
    /// </summary>
    /// <param name="amount">The integer <paramref name="amount"/> to add to the meter.</param>
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

    /// <summary>
    /// A generic utility demonstrating advanced type extraction for localized components.
    /// </summary>
    /// <typeparam name="T">The component type to extract.</typeparam>
    /// <returns>Returns the component of type <typeparamref name="T"/> attached to this object.</returns>
    /// <exception cref="System.NullReferenceException">Thrown if the component cannot be found.</exception>
    public T GetComponentSafely<T>() where T : Component
    {
        if (!TryGetComponent<T>(out T component))
        {
            throw new System.NullReferenceException($"Component {typeof(T)} not found.");
        }
        return component;
    }
}