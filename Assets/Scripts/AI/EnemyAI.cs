using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Controls the programmatic decision-making logic for the AI antagonist.
/// </summary>
public class EnemyAI : MonoBehaviour
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

    /// <summary>Caches components prior to first frame.</summary>
    private void Awake()
    {
        TryGetComponent<Animator>(out aiAnimator);
    }

    /// <summary>Subscribes to turn management events to listen for AI trigger.</summary>
    private void OnEnable()
    {
        turnManager.OnTurnChanged.AddListener(HandleTurnChange);
    }

    /// <summary>Unsubscribes from turn management events to prevent memory leaks.</summary>
    private void OnDisable()
    {
        turnManager.OnTurnChanged.RemoveListener(HandleTurnChange);
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
    /// Coroutine that evaluates battlefield conditions and selects an optimal move based on hardcoded thresholds.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> handling the execution delay.</returns>
    private IEnumerator ExecuteEnemyTurnSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (playerHealth.GetHealthPercentage() < 0.3f)
        {
            OnAIDecisionMade?.Invoke("The Beast lunges for a heavy bite!");
            aiAnimator.SetTrigger("SpecialAttack");
        }
        else if (enemyHealth.GetHealthPercentage() < 0.5f)
        {
            OnAIDecisionMade?.Invoke("The Beast howls, regenerating health!");
            aiAnimator.SetTrigger("Defend");
            enemyHealth.Heal(howlHealAmount);
            EndAITurn();
        }
        else
        {
            OnAIDecisionMade?.Invoke("The Beast swipes its claws!");
            aiAnimator.SetTrigger("Attack");
        }
    }

    /// <summary>Applies damage for the basic AI attack (Triggered via Animation Event).</summary>
    public void ExecuteStandardDamage()
    {
        playerHealth.TakeDamage(clawDamage);
        EndAITurn();
    }

    /// <summary>Applies damage for the heavy AI attack (Triggered via Animation Event).</summary>
    public void ExecuteHeavyDamage()
    {
        playerHealth.TakeDamage(heavyBiteDamage);
        EndAITurn();
    }

    /// <summary>Returns control to the player by requesting a state change.</summary>
    private void EndAITurn()
    {
        turnManager.SwitchTurn(TurnState.PlayerTurn);
    }
}