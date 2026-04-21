using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines standard implementation requirements for objects that must listen to global turn state changes.
/// </summary>
public interface ITurnListener
{
    /// <summary>
    /// Reacts to a broadcasted change in the game's state machine.
    /// </summary>
    /// <param name="newState">The incoming turn state enumeration.</param>
    void OnTurnStateChanged(CombatSettings.TurnState newState);
}

/// <summary>
/// A decoupled component responsible for managing hit points and broadcasting damage or healing events.
/// </summary>
/// <remarks>
/// <para>This class acts as the central data hub for an entity's vitality. It relies entirely on the <see cref="UnityEvent"/> system to communicate with the UI and visual effect spawners, adhering strictly to decoupled architecture principles.</para>
/// <include file='ExternalDocs.xml' path='docs/members[@name="Health"]/Health/*'/>
/// </remarks>
public class Health : MonoBehaviour, ITurnListener
{
    [Header("Core Vitals")]
    /// <summary>The maximum possible hit points for this entity.</summary>
    [SerializeField] private float maxHealth = 100f;

    /// <summary>The entity's current hit points.</summary>
    [SerializeField] private float currentHealth;

    /// <summary>Internal flag to determine if the entity is currently mitigating incoming damage.</summary>
    private bool isDefending = false;

    /// <summary>Exposes the current raw hit point value.</summary>
    /// <value>The current HP as a float.</value>
    public float CurrentHealth => currentHealth;

    [Header("Broadcasting Events")]
    /// <summary>Event fired whenever health increases or decreases. Passes current and max health for UI sliders.</summary>
    public UnityEvent<float, float> OnHealthChanged;

    [Header("Dependencies")]
    /// <summary>Reference to the global turn authority used to expire per-turn buffs.</summary>
    [SerializeField] private TurnManager turnManager;

    /// <summary>Event fired when damage is successfully applied. Passes the damage amount and critical hit status for floating text.</summary>
    public UnityEvent<float, bool> OnDamageTaken;

    /// <summary>Event fired when hit points are successfully restored. Passes the healed amount for floating text.</summary>
    public UnityEvent<float> OnHealed;

    /// <summary>Event fired when <see cref="currentHealth"/> drops to or below zero.</summary>
    public UnityEvent OnDeath;

    /// <summary>Initializes health variables before the first frame execution.</summary>
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>Broadcasts the initial health state to all registered listeners upon startup.</summary>
    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reduces the entity's current health by the specified numerical amount.
    /// </summary>
    /// <param name="damageAmount">The raw numerical damage value to apply to the health pool.</param>
    /// <param name="isCrit">A boolean flag indicating if the incoming attack was calculated as a critical hit.</param>
    /// <remarks>
    /// If the <see cref="isDefending"/> flag evaluates to <c>true</c>, the <paramref name="damageAmount"/> is immediately reduced by 50%.
    /// </remarks>
    public void TakeDamage(float damageAmount, bool isCrit = false)
    {
        if (isDefending)
        {
            damageAmount *= 0.5f;
            isDefending = false;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(damageAmount, isCrit);

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Increases the entity's current health, strictly capped at the <see cref="maxHealth"/> limit.
    /// </summary>
    /// <param name="healAmount">The numerical amount of hit points to restore.</param>
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHealed?.Invoke(healAmount); // Broadcast the new healing event!
    }

    /// <summary>
    /// Toggles the entity's defensive multiplier flag.
    /// </summary>
    /// <param name="state">Pass <c>true</c> to enable defense mode; otherwise, pass <c>false</c>.</param>
    public void SetDefending(bool state)
    {
        isDefending = state;
    }

    /// <summary>
    /// Calculates the current health as a normalized mathematical fraction.
    /// </summary>
    /// <returns>Returns a float value strictly between 0.0 and 1.0 representing the health percentage.</returns>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Subscribes to the global turn authority so this entity can react to state transitions.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="ITurnListener"/> contract requires this component to respond when the combat phase changes —
    /// specifically, to expire the one-turn <see cref="isDefending"/> buff at the start of each new player turn.</para>
    /// <para>Subscription happens in <c>OnEnable</c> rather than <c>Awake</c> so that the listener chain is correctly
    /// re-established if the GameObject is ever deactivated and reactivated at runtime.</para>
    /// </remarks>
    private void OnEnable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.AddListener(OnTurnStateChanged);
    }

    /// <summary>
    /// Unsubscribes from the turn authority to prevent memory leaks and ghost invocations.
    /// </summary>
    /// <remarks>
    /// Mirrors the subscription in <see cref="OnEnable"/>. Failing to remove listeners when a component is disabled
    /// or destroyed is a common source of <see cref="System.NullReferenceException"/> in Unity event-driven architectures,
    /// because Unity will continue broadcasting to destroyed targets until the event is manually cleaned up.
    /// </remarks>
    private void OnDisable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.RemoveListener(OnTurnStateChanged);
    }

    /// <inheritdoc/>
    public void OnTurnStateChanged(CombatSettings.TurnState newState)
    {
        // Defense is a one-turn buff — expire it whenever a new player turn begins,
        // regardless of whether the enemy actually attacked last turn.

        if (newState == CombatSettings.TurnState.PlayerTurn)
            {
                isDefending = false;
            }

        if (newState == CombatSettings.TurnState.GameOver)
            {
                isDefending = false;
            }
    }
}