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

    [Header("Broadcasting Events")]
    /// <summary>Event fired whenever health increases or decreases. Passes current and max health for UI sliders.</summary>
    public UnityEvent<float, float> OnHealthChanged;

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

    /// <inheritdoc/>
    public void OnTurnStateChanged(CombatSettings.TurnState newState)
    {
        if (newState == CombatSettings.TurnState.GameOver)
        {
            isDefending = false;
        }
    }
}