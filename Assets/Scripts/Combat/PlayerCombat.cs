using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Defines standard operational requirements for player-controlled entities within the combat architecture.
/// </summary>
public interface IPlayerCombat
{
    /// <summary>
    /// Executes the primary melee sequence logic.
    /// </summary>
    void Action_CaneWhack();
}

/// <summary>
/// Manages the protagonist's specific combat actions, including RNG damage modifiers and animation routing.
/// </summary>
/// <remarks>
/// <para>Damage values are dynamically calculated per turn using <see cref="Random.Range"/>.</para>
/// <list type="bullet">
/// <item><term>Animation Driven</term><description>Damage is applied via Animation Events when an animator is active.</description></item>
/// <item><term>Fallback Support</term><description>If no animator is present, damage and turn progression execute instantly.</description></item>
/// </list>
/// <example>
/// <code>
/// // Example of UI Event triggering the primary sequence:
/// myPlayerCombat.Action_CaneWhack();
/// </code>
/// </example>
/// <include file='ExternalDocs.xml' path='docs/members[@name="PlayerCombat"]/PlayerCombat/*'/>
/// </remarks>
/// <seealso cref="TurnManager"/>
public class PlayerCombat : MonoBehaviour, IPlayerCombat
{
    [Header("Dependencies")]
    /// <summary>Reference to the global state authority.</summary>
    [SerializeField] private TurnManager turnManager;
    /// <summary>Reference to the antagonist's vital component.</summary>
    [SerializeField] private Health enemyHealth;
    /// <summary>Reference to the protagonist's vital component.</summary>
    [SerializeField] private Health playerHealth;

    [Header("Combat Stats & RNG")]
    /// <summary>Minimum randomized boundary for basic attack damage.</summary>
    [SerializeField] private float minCaneDamage = 12f;
    /// <summary>Maximum randomized boundary for basic attack damage.</summary>
    [SerializeField] private float maxCaneDamage = 20f;

    /// <summary>Minimum randomized boundary for the ultimate attack damage.</summary>
    [SerializeField] private float minPurseDamage = 35f;
    /// <summary>Maximum randomized boundary for the ultimate attack damage.</summary>
    [SerializeField] private float maxPurseDamage = 50f;

    /// <summary>Probability of striking a critical hit, represented as a float between 0.0 and 1.0.</summary>
    [SerializeField, Range(0f, 1f)] private float critChance = 0.15f;
    /// <summary>The scaling multiplier applied to the base damage upon a successful critical hit.</summary>
    [SerializeField] private float critMultiplier = 1.5f;

    /// <summary>Minimum boundary for hit points restored by the healing action.</summary>
    [SerializeField] private float minCookieHealAmount = 15f;
    /// <summary>Maximum boundary for hit points restored by the healing action.</summary>
    [SerializeField] private float maxCookieHealAmount = 30f;

    /// <summary>The maximum capacity of the ultimate ability meter.</summary>
    [SerializeField] private int maxGrandmaMeter = 3;

    /// <summary>The current charge level of the ultimate ability meter.</summary>
    private int currentGrandmaMeter = 0;
    /// <summary>Tracks hit points internally to determine if damage was taken during a turn cycle.</summary>
    private float previousHealth;

    /// <summary>Internal flag to prevent the player from spamming actions during a single turn.</summary>
    private bool isActionLocked = false;

    [Header("Broadcasting Events")]
    /// <summary>Broadcasts state changes to the ultimate meter for UI slider updates.</summary>
    public UnityEvent<int, int> OnMeterUpdated;

    /// <summary>Broadcasts instantly when an action is selected to notify the UI to lock buttons.</summary>
    public UnityEvent OnActionStarted;

    /// <summary>The animator component driving visual feedback states.</summary>
    private Animator characterAnimator;

    /// <summary>Exposes the current meter charge mathematically.</summary>
    /// <value>Returns an integer representing the <c>currentGrandmaMeter</c> numeric charge.</value>
    public int CurrentMeter => currentGrandmaMeter;

    /// <summary>Caches essential components prior to initial execution.</summary>
    /// <exception cref="UnityEngine.MissingComponentException">Thrown if no Animator component is attached to the parent GameObject.</exception>
    private void Awake()
    {
        if (!TryGetComponent<Animator>(out characterAnimator))
        {
            throw new MissingComponentException("Animator missing on Player object.");
        }
    }

    /// <summary>Initializes tracking values against actual starting HP.</summary>
    private void Start()
    {
        if (playerHealth != null) previousHealth = playerHealth.CurrentHealth;
    }

    /// <summary>Subscribes to turn and health events.</summary>
    private void OnEnable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.AddListener(HandleTurnChange);

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            playerHealth.OnDeath.AddListener(HandleDeath);
        }
    }

    /// <summary>Unsubscribes from events to prevent memory degradation.</summary>
    private void OnDisable()
    {
        if (turnManager != null) turnManager.OnTurnChanged.RemoveListener(HandleTurnChange);

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            playerHealth.OnDeath.RemoveListener(HandleDeath);
        }
    }

    /// <summary>
    /// Intercepts state changes to unlock actions when the player's turn begins.
    /// </summary>
    /// <param name="newState">The newly broadcasted state from the turn manager.</param>
    private void HandleTurnChange(TurnState newState)
    {
        if (newState == TurnState.PlayerTurn)
        {
            isActionLocked = false;
        }
    }

    /// <inheritdoc/>
    public void Action_CaneWhack()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || isActionLocked) return;
        LockAction();

        UpdateGrandmaMeter(1);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyAttack");
        else ExecuteMeleeDamage();
    }

    /// <summary>Initiates the defensive block sequence and concludes the player phase.</summary>
    public void Action_KnittingShield()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || isActionLocked) return;
        LockAction();

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyBlock");
        else ExecuteBlock();
    }

    /// <summary>Initiates the healing sequence and concludes the player phase.</summary>
    public void Action_BakeCookies()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || isActionLocked) return;
        LockAction();

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyHeal");
        else ExecuteHeal();
    }

    /// <summary>Initiates the ultimate attack sequence if the meter is fully charged.</summary>
    public void Action_PurseSlam()
    {
        if (turnManager.CurrentTurn != TurnState.PlayerTurn || isActionLocked || currentGrandmaMeter < maxGrandmaMeter) return;
        LockAction();

        currentGrandmaMeter = 0;
        OnMeterUpdated?.Invoke(currentGrandmaMeter, maxGrandmaMeter);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannySpecial");
        else ExecuteSpecialDamage();
    }

    /// <summary>
    /// Applies the internal lock flag and broadcasts the UI lock event.
    /// </summary>
    private void LockAction()
    {
        isActionLocked = true;
        OnActionStarted?.Invoke();
    }

    /// <summary>
    /// Evaluates if the player sustained damage to trigger the appropriate reaction animation.
    /// </summary>
    /// <param name="current">The current hit points broadcasted by the listener.</param>
    /// <param name="max">The maximum hit points broadcasted by the listener.</param>
    private void HandleHealthChanged(float current, float max)
    {
        if (current < previousHealth && characterAnimator != null)
        {
            characterAnimator.SetTrigger("GrannyHurt");
        }
        previousHealth = current;
    }

    /// <summary>Triggers the terminal animation sequence upon health reaching zero.</summary>
    private void HandleDeath()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyDeath");
    }

    /// <summary>
    /// Calculates RNG-based damage output and evaluates critical hit logic.
    /// </summary>
    /// <param name="min">The minimum boundary for the damage roll.</param>
    /// <param name="max">The maximum boundary for the damage roll.</param>
    /// <param name="isCrit">An out parameter that returns <c>true</c> if the calculation resulted in a critical hit.</param>
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

    /// <summary>Executes the mathematical damage application for a basic attack.</summary>
    /// <remarks>Must be triggered by an Animation Event if an Animator is present.</remarks>
    public void ExecuteMeleeDamage()
    {
        float finalDamage = CalculateDamage(minCaneDamage, maxCaneDamage, out bool isCrit);
        enemyHealth.TakeDamage(finalDamage, isCrit);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>Executes the mathematical damage application for the ultimate attack.</summary>
    /// <remarks>Must be triggered by an Animation Event if an Animator is present.</remarks>
    public void ExecuteSpecialDamage()
    {
        float finalDamage = CalculateDamage(minPurseDamage, maxPurseDamage, out bool isCrit);
        enemyHealth.TakeDamage(finalDamage, isCrit);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>Executes the mathematical healing logic.</summary>
    /// <remarks>Must be triggered by an Animation Event if an Animator is present.</remarks>
    public void ExecuteHeal()
    {
        float finalHeal = Random.Range(minCookieHealAmount, maxCookieHealAmount);
        playerHealth.Heal(finalHeal);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>Executes the mathematical defense buff logic.</summary>
    /// <remarks>Must be triggered by an Animation Event if an Animator is present.</remarks>
    public void ExecuteBlock()
    {
        playerHealth.SetDefending(true);

        if (characterAnimator != null) characterAnimator.SetTrigger("GrannyIdle");
        EndTurn();
    }

    /// <summary>Modifies the ultimate meter logic and broadcasts the updated state.</summary>
    /// <param name="amount">The integer <paramref name="amount"/> to append to the current meter.</param>
    private void UpdateGrandmaMeter(int amount)
    {
        currentGrandmaMeter += amount;
        currentGrandmaMeter = Mathf.Clamp(currentGrandmaMeter, 0, maxGrandmaMeter);
        OnMeterUpdated?.Invoke(currentGrandmaMeter, maxGrandmaMeter);
    }

    /// <summary>Concludes the protagonist's action phase and passes control to the antagonist.</summary>
    private void EndTurn()
    {
        turnManager.SwitchTurn(TurnState.EnemyTurn);
    }

    /// <summary>
    /// A generic utility demonstrating advanced type extraction for localized component architecture.
    /// </summary>
    /// <typeparam name="T">The specific component type to extract.</typeparam>
    /// <returns>Returns the component of type <typeparamref name="T"/> attached to this object instance.</returns>
    /// <exception cref="System.NullReferenceException">Thrown if the specified component type cannot be located.</exception>
    public T GetComponentSafely<T>() where T : Component
    {
        if (!TryGetComponent<T>(out T component))
        {
            throw new System.NullReferenceException($"Component {typeof(T)} not found on target object.");
        }
        return component;
    }
}