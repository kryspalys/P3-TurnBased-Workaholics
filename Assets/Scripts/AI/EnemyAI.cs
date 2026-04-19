using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Defines standard AI controller requirements.
/// </summary>
public interface IEnemyAI
{
    /// <summary>
    /// Applies standard damage to the player target.
    /// </summary>
    void ExecuteStandardDamage();
}

/// <summary>
/// Controls the programmatic decision-making logic for the AI antagonist.
/// </summary>
/// <remarks>
/// <para>This class handles automated responses and ensures animations map perfectly to AI states.</para>
/// <list type="bullet">
/// <item><term>Threshold Logic</term><description>Decisions are hardcoded based on HP percentages.</description></item>
/// <item><term>Event Driven</term><description>Subscribes to state changes rather than polling in Update loops.</description></item>
/// </list>
/// <example>
/// <code>
/// // Implicitly started by TurnManager event broadcasts.
/// </code>
/// </example>
/// <include file='ExternalDocs.xml' path='docs/members[@name="EnemyAI"]/EnemyAI/*'/>
/// </remarks>
/// <seealso cref="Health"/>
public class EnemyAI : MonoBehaviour, IEnemyAI
{
    [Header("Dependencies")]
    /// <summary>Reference to the global turn authority.</summary>
    [SerializeField] private TurnManager turnManager;
    /// <summary>Reference to the AI's health component.</summary>
    [SerializeField] private Health enemyHealth;
    /// <summary>Reference to the player's health component.</summary>
    [SerializeField] private Health playerHealth;

    [Header("Stats")]
    /// <summary>Damage dealt by the standard attack.</summary>
    [SerializeField] private float clawDamage = 12f;
    /// <summary>Damage dealt by the aggressive threshold attack.</summary>
    [SerializeField] private float heavyBiteDamage = 25f;
    /// <summary>Amount healed when defensive threshold is met.</summary>
    [SerializeField] private float howlHealAmount = 20f;

    /// <summary>Broadcasts the AI's intended action as a text string to the UI.</summary>
    public UnityEvent<string> OnAIDecisionMade;

    /// <summary>The animator driving the AI's visual state.</summary>
    private Animator aiAnimator;

    /// <summary>Tracks hit points to determine if damage was taken.</summary>
    private float previousHealth;

    /// <summary>Exposes the current configured claw damage.</summary>
    /// <value>Returns a float representing the base <c>clawDamage</c>.</value>
    public float BaseDamage => clawDamage;

    /// <summary>Caches components prior to first frame.</summary>
    private void Awake()
    {
        TryGetComponent<Animator>(out aiAnimator);
    }

    /// <summary>Subscribes to turn management and health events.</summary>
    private void OnEnable()
    {
        turnManager.OnTurnChanged.AddListener(HandleTurnChange);
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            enemyHealth.OnDeath.AddListener(HandleDeath);
        }
    }

    /// <summary>Unsubscribes from events to prevent memory leaks.</summary>
    private void OnDisable()
    {
        turnManager.OnTurnChanged.RemoveListener(HandleTurnChange);
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            enemyHealth.OnDeath.RemoveListener(HandleDeath);
        }
    }

    /// <summary>
    /// Intercepts state changes and initiates the AI coroutine if it is the enemy's turn.
    /// </summary>
    /// <param name="newState">The newly broadcasted turn state.</param>
    private void HandleTurnChange(TurnState newState)
    {
        if (newState == TurnState.EnemyTurn)
        {
            StartCoroutine(ExecuteEnemyTurnSequence());
        }
    }

    /// <summary>
    /// Evaluates if the AI took damage to trigger the appropriate reaction.
    /// </summary>
    /// <param name="current">The current hit points broadcasted by the health script.</param>
    /// <param name="max">The maximum hit points broadcasted by the health script.</param>
    private void HandleHealthChanged(float current, float max)
    {
        if (current < previousHealth && aiAnimator != null)
        {
            aiAnimator.SetTrigger("WolfHurt");
        }
        previousHealth = current;
    }

    /// <summary>
    /// Triggers the terminal animation sequence upon health reaching zero.
    /// </summary>
    private void HandleDeath()
    {
        if (aiAnimator != null) aiAnimator.SetTrigger("WolfDeath");
    }

    /// <summary>
    /// Coroutine that evaluates battlefield conditions and selects an optimal move based on hardcoded thresholds.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> handling the execution delay.</returns>
    private IEnumerator ExecuteEnemyTurnSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (playerHealth.GetHealthPercentage() < 0.3f)
        {
            OnAIDecisionMade?.Invoke("The Beast lunges for a heavy bite!");

            if (aiAnimator != null) aiAnimator.SetTrigger("WolfAttack");
            else ExecuteHeavyDamage();
        }
        else if (enemyHealth.GetHealthPercentage() < 0.5f)
        {
            OnAIDecisionMade?.Invoke("The Beast howls, regenerating health!");

            if (aiAnimator != null) aiAnimator.SetTrigger("WolfHeal");

            enemyHealth.Heal(howlHealAmount);
            EndAITurn();
        }
        else
        {
            OnAIDecisionMade?.Invoke("The Beast swipes its claws!");

            if (aiAnimator != null) aiAnimator.SetTrigger("WolfAttack");
            else ExecuteStandardDamage();
        }
    }

    /// <inheritdoc/>
    public void ExecuteStandardDamage()
    {
        playerHealth.TakeDamage(clawDamage);
        if (aiAnimator != null) aiAnimator.SetTrigger("WolfIdle");
        EndAITurn();
    }

    /// <summary>Applies damage for the heavy AI attack (Triggered via Animation Event).</summary>
    public void ExecuteHeavyDamage()
    {
        playerHealth.TakeDamage(heavyBiteDamage);
        if (aiAnimator != null) aiAnimator.SetTrigger("WolfIdle");
        EndAITurn();
    }

    /// <summary>Returns control to the player by requesting a state change.</summary>
    private void EndAITurn()
    {
        turnManager.SwitchTurn(TurnState.PlayerTurn);
    }

    /// <summary>
    /// A generic utility demonstrating advanced type extraction.
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