using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines standard implementation for objects that listen to turn state changes.
/// </summary>
public interface ITurnListener
{
    /// <summary>
    /// Reacts to a change in the game's turn state.
    /// </summary>
    /// <param name="newState">The incoming turn state.</param>
    void OnTurnStateChanged(CombatSettings.TurnState newState);
}

/// <summary>
/// A decoupled component responsible for managing hit points and broadcasting damage/healing events.
/// </summary>
/// <remarks>
/// This class relies entirely on <see cref="UnityEvent"/> to communicate with the UI, adhering to strict decoupling principles.
/// </remarks>
public class Health : MonoBehaviour, ITurnListener
{
    /// <summary>The maximum possible hit points for this entity.</summary>
    [SerializeField] private float maxHealth = 100f;

    /// <summary>The entity's current hit points.</summary>
    [SerializeField] private float currentHealth;

    /// <summary>Internal flag to determine if the entity is mitigating incoming damage.</summary>
    private bool isDefending = false;

    /// <summary>Event fired whenever health increases or decreases. Passes current and max health.</summary>
    [Header("Health Events")]
    public UnityEvent<float, float> OnHealthChanged;

    /// <summary>Event fired when <see cref="currentHealth"/> drops to zero.</summary>
    public UnityEvent OnDeath;

    /// <summary>Initializes health variables before the first frame.</summary>
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>Broadcasts the initial health state to listeners.</summary>
    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reduces the entity's current health by the specified amount.
    /// </summary>
    /// <param name="damageAmount">The raw numerical damage to apply.</param>
    /// <remarks>
    /// If <see cref="isDefending"/> is true, <paramref name="damageAmount"/> is reduced by 50%.
    /// </remarks>
    public void TakeDamage(float damageAmount)
    {
        if (isDefending)
        {
            damageAmount *= 0.5f;
            isDefending = false;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Increases the entity's current health, capped at <see cref="maxHealth"/>.
    /// </summary>
    /// <param name="healAmount">The numerical amount of hit points to restore.</param>
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Toggles the defense multiplier flag.
    /// </summary>
    /// <param name="state">True to enable defense mode; otherwise, false.</param>
    public void SetDefending(bool state)
    {
        isDefending = state;
    }

    /// <summary>
    /// Calculates the current health as a normalized fraction.
    /// </summary>
    /// <returns>A float between 0.0 and 1.0 representing the health percentage.</returns>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <inheritdoc/>
    public void OnTurnStateChanged(CombatSettings.TurnState newState)
    {
        // Example implementation: could reset statuses based on turn shifts.
        if (newState == CombatSettings.TurnState.GameOver)
        {
            isDefending = false;
        }
    }
}