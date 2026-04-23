using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Defines standard controller requirements for programmatic antagonists.
/// </summary>
public interface IEnemyAI
{
    /// <summary>
    /// Executes the standard mathematical damage application to the player target.
    /// </summary>
    void ExecuteStandardDamage();
}

/// <summary>
/// Controls the programmatic decision-making logic for the AI antagonist.
/// </summary>
/// <remarks>
/// <para>This class handles automated responses and ensures animations map perfectly to the underlying mathematical state.</para>
/// <list type="bullet">
/// <item><term>Threshold Logic</term><description>Decisions are dynamically hardcoded based on HP percentages.</description></item>
/// <item><term>Event Driven</term><description>Subscribes to global state changes rather than polling heavily in continuous loops.</description></item>
/// </list>
/// <example>
/// <code>
/// // Implicitly started by TurnManager event broadcasts; no direct calls required:
/// // turnManager.OnTurnChanged.AddListener(HandleTurnChange);
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
    /// <summary>Reference to the AI's internal vital component.</summary>
    [SerializeField] private Health enemyHealth;
    /// <summary>Reference to the player's external vital component.</summary>
    [SerializeField] private Health playerHealth;

    [Header("Combat Stats & RNG")]
    /// <summary>Minimum randomized boundary for basic attack damage.</summary>
    [SerializeField] private float minClawDamage = 12f;
    /// <summary>Maximum randomized boundary for basic attack damage.</summary>
    [SerializeField] private float maxClawDamage = 18f;

    /// <summary>Minimum randomized boundary for the threshold aggressive attack.</summary>
    [SerializeField] private float minBiteDamage = 22f;
    /// <summary>Maximum randomized boundary for the threshold aggressive attack.</summary>
    [SerializeField] private float maxBiteDamage = 32f;

    /// <summary>Probability of striking a critical hit, represented as a float between 0.0 and 1.0.</summary>
    [SerializeField, Range(0f, 1f)] private float critChance = 0.10f;
    /// <summary>The scaling multiplier applied to the base damage upon a successful critical hit.</summary>
    [SerializeField] private float critMultiplier = 1.5f;

    /// <summary>Minimum amount of hit points restored when the defensive threshold is met.</summary>
    [SerializeField] private float minHowlHealAmount = 25f;
    /// <summary>Maximum amount of hit points restored when the defensive threshold is met.</summary>
    [SerializeField] private float maxHowlHealAmount = 35f;

    [Header("Heal Restrictions")]
    /// <summary>Maximum number of times the heal ability can be used in a single combat encounter.</summary>
    [SerializeField] private int maxHealUses = 2;

    /// <summary>Number of enemy turns that must elapse between heal uses.</summary>
    [SerializeField] private int healCooldownTurns = 3;

    /// <summary>Tracks remaining heal uses for this fight. Decrements on each successful heal.</summary>
    private int healUsesRemaining;

    /// <summary>Tracks the cooldown counter. Decrements at the start of each enemy turn; heal is locked until this reaches zero.</summary>
    private int healCooldownRemaining = 0;

    [Header("Broadcasting Events")]
    /// <summary>Broadcasts the AI's intended action as a text string to the UI listener.</summary>
    public UnityEvent<string> OnAIDecisionMade;

    /// <summary>Broadcasts the current and maximum heal uses whenever the potion count changes. Used by UI listeners.</summary>
    public UnityEvent<int, int> OnHealUsesChanged;

    /// <summary>Broadcasts the current and maximum cooldown whenever the cooldown state changes. Used by UI listeners.</summary>
    public UnityEvent<int, int> OnHealCooldownChanged;

    /// <summary>Broadcast when the wolf executes a basic claw attack. Listened to by audio, VFX, etc.</summary>
    public UnityEvent OnClawAttackUsed;

    /// <summary>Broadcast when the wolf executes the heavy bite attack.</summary>
    public UnityEvent OnBiteAttackUsed;

    /// <summary>Broadcast when the wolf executes the self-heal howl.</summary>
    public UnityEvent OnHowlUsed;

    /// <summary>The animator component driving the AI's visual state.</summary>
    private Animator aiAnimator;

    /// <summary>Tracks hit points internally to determine if damage was taken during a cycle.</summary>
    private float previousHealth;

    /// <summary>Exposes the current minimum configured claw damage.</summary>
    /// <value>Returns a float representing the base <c>minClawDamage</c>.</value>
    public float BaseDamage => minClawDamage;

    /// <summary>Caches components prior to first frame execution.</summary>
    private void Awake()
    {
        TryGetComponent<Animator>(out aiAnimator);
        healUsesRemaining = maxHealUses;
    }

    /// <summary>Subscribes to turn management and health events to establish the listener chain.</summary>
    private void OnEnable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.AddListener(HandleTurnChange);

        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            enemyHealth.OnDeath.AddListener(HandleDeath);
        }
    }

    /// <summary>
    /// Captures the initial health baseline after all Awake calls resolve, then broadcasts
    /// the initial heal resource state to any UI listeners.
    /// </summary>
    /// <remarks>
    /// Running this in Start rather than Awake guarantees that Health.currentHealth has been
    /// initialized by its own Awake before we read it, regardless of script execution order.
    /// </remarks>
    private void Start()
    {
        if (enemyHealth != null) previousHealth = enemyHealth.CurrentHealth;
        OnHealUsesChanged?.Invoke(healUsesRemaining, maxHealUses);
        OnHealCooldownChanged?.Invoke(healCooldownRemaining, healCooldownTurns);
    }

    /// <summary>Unsubscribes from events to prevent memory leaks upon disable or destruction.</summary>
    private void OnDisable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.RemoveListener(HandleTurnChange);

        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            enemyHealth.OnDeath.RemoveListener(HandleDeath);
        }
    }

    /// <summary>
    /// Intercepts state changes and initiates the AI coroutine if it is the enemy's designated turn.
    /// </summary>
    /// <param name="newState">The newly broadcasted state from the turn manager.</param>
    private void HandleTurnChange(TurnState newState)
    {
        Debug.Log($"[TRACE 3] EnemyAI heard Turn Change: {newState}");

        if (newState == TurnState.EnemyTurn)
        {
            StartCoroutine(ExecuteEnemyTurnSequence());
        }
    }

    /// <summary>
    /// Evaluates if the AI sustained damage to trigger the appropriate reaction animation.
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
    /// Evaluates battlefield conditions and selects an action based on thresholds and resource constraints.
    /// </summary>
    /// <remarks>
    /// <para>Decision priorities, in order:</para>
    /// <list type="number">
    /// <item><description>If the player is at critical HP (&lt; 30%), lunge for a heavy bite to secure the kill.</description></item>
    /// <item><description>Otherwise, if own HP is low (&lt; 50%) AND heal is off cooldown AND uses remain, self-heal.</description></item>
    /// <item><description>Otherwise, use a standard claw attack.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>An <see cref="IEnumerator"/> handling the deliberation delay.</returns>
    private IEnumerator ExecuteEnemyTurnSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (turnManager.CurrentTurn != TurnState.EnemyTurn) yield break;

        // Tick the cooldown down at the start of each enemy turn before evaluating decisions.
        if (healCooldownRemaining > 0)
        {
            healCooldownRemaining--;
            OnHealCooldownChanged?.Invoke(healCooldownRemaining, healCooldownTurns);
        }

        bool canHeal = healUsesRemaining > 0 && healCooldownRemaining <= 0;

        if (playerHealth.GetHealthPercentage() < 0.3f)
        {
            OnAIDecisionMade?.Invoke("The Beast lunges for a heavy bite!");
            // BUG FIX: Changed from "WolfAttack" to "WolfBite" so it uses the correct animation and event!
            if (aiAnimator != null) aiAnimator.SetTrigger("WolfBite");
            else ExecuteHeavyDamage();
        }
        else if (enemyHealth.GetHealthPercentage() < 0.5f && canHeal)
        {
            OnAIDecisionMade?.Invoke("The Beast howls, regenerating health!");
            if (aiAnimator != null) aiAnimator.SetTrigger("WolfHeal");

            float finalHeal = Random.Range(minHowlHealAmount, maxHowlHealAmount);
            enemyHealth.Heal(finalHeal);

            healUsesRemaining--;
            healCooldownRemaining = healCooldownTurns;
            OnHealUsesChanged?.Invoke(healUsesRemaining, maxHealUses);
            OnHealCooldownChanged?.Invoke(healCooldownRemaining, healCooldownTurns);

            OnHowlUsed?.Invoke();

            EndAITurn();
        }
        else
        {
            OnAIDecisionMade?.Invoke("The Beast swipes its claws!");
            if (aiAnimator != null) aiAnimator.SetTrigger("WolfAttack");
            else ExecuteStandardDamage();
        }
    }

    /// <summary>
    /// Calculates RNG-based damage and evaluates critical hits against the player.
    /// </summary>
    /// <param name="min">The minimum boundary for the damage roll.</param>
    /// <param name="max">The maximum boundary for the damage roll.</param>
    /// <param name="isCrit">Outputs <c>true</c> if the calculation resulted in a critical hit.</param>
    /// <returns>Returns the final calculated float damage value.</returns>
    private float CalculateDamage(float min, float max, out bool isCrit)
    {
        float damage = Random.Range(min, max);
        isCrit = Random.value <= critChance;

        if (isCrit)
        {
            damage *= critMultiplier;
        }

        return damage;
    }

    /// <inheritdoc/>
    /// <remarks>Must be triggered via Animation Event if an animator is present.</remarks>
    public void ExecuteStandardDamage()
    {
        float finalDamage = CalculateDamage(minClawDamage, maxClawDamage, out bool isCrit);
        playerHealth.TakeDamage(finalDamage, isCrit);

        OnClawAttackUsed?.Invoke();

        if (aiAnimator != null) aiAnimator.SetTrigger("WolfIdle");
        EndAITurn();
    }

    /// <summary>
    /// Applies heavily scaled damage for the AI's special threshold attack.
    /// </summary>
    /// <remarks>Must be triggered via Animation Event if an animator is present.</remarks>
    public void ExecuteHeavyDamage()
    {
        float finalDamage = CalculateDamage(minBiteDamage, maxBiteDamage, out bool isCrit);
        playerHealth.TakeDamage(finalDamage, isCrit);

        OnBiteAttackUsed?.Invoke();

        if (aiAnimator != null) aiAnimator.SetTrigger("WolfIdle");
        EndAITurn();
    }

    /// <summary>
    /// Returns control to the player interface by requesting a state change from the global manager.
    /// </summary>
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
            throw new System.NullReferenceException($"Component {typeof(T)} not found on target object.");
        }
        return component;
    }
}