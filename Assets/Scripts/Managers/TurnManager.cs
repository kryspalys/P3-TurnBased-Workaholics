using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// Defines standard operations for managing sequential combat phases.
/// </summary>
public interface ITurnManager
{
    /// <summary>
    /// Transitions the game to a newly specified combat phase.
    /// </summary>
    /// <param name="newState">The incoming state to apply.</param>
    void SwitchTurn(TurnState newState);
}

/// <summary>
/// The central controller that manages the flow of <see cref="TurnState"/> transitions.
/// </summary>
/// <remarks>
/// <para>This class enforces the strict turn order required by the decoupled game architecture. 
/// No peripheral script is permitted to change the turn state directly; they must request a mutation through this manager.</para>
/// <include file='ExternalDocs.xml' path='docs/members[@name="TurnManager"]/TurnManager/*'/>
/// </remarks>
public class TurnManager : MonoBehaviour, ITurnManager
{
    [Header("State Data")]
    /// <summary>
    /// The current operational state of the combat loop.
    /// </summary>
    [SerializeField] private TurnState currentTurnState = TurnState.PlayerTurn;

    /// <summary>
    /// Internal flag to track if the initial state broadcast has occurred, preventing redundant updates.
    /// </summary>
    private bool hasInitialized = false;

    [Header("Broadcasting Events")]
    /// <summary>
    /// Broadcasts an event containing the new <see cref="TurnState"/> whenever the state mutates.
    /// </summary>
    public UnityEvent<TurnState> OnTurnChanged;

    /// <summary>
    /// Gets the currently active combat phase.
    /// </summary>
    /// <value>Returns the active <see cref="TurnState"/> enumeration.</value>
    public TurnState CurrentTurn => currentTurnState;

    /// <summary>
    /// Initializes the combat loop by forcing the initial state broadcast prior to player interaction.
    /// </summary>
    /// <remarks>
    /// Uses an IEnumerator to wait one frame. This guarantees all other scripts (Health, EnemyAI, UI) 
    /// have completed their Start() methods and are actively listening before the first turn fires.
    /// </remarks>
    private System.Collections.IEnumerator Start()
    {
        // Wait for the end of the current frame's initialization phase
        yield return null;

        SwitchTurn(TurnState.PlayerTurn);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>If the <paramref name="newState"/> or the <see cref="currentTurnState"/> is evaluated as <c>TurnState.GameOver</c>, further transitions are permanently blocked.</para>
    /// <para>Includes a safeguard to prevent consecutive broadcasts of the same state, which averts duplicate routine execution.</para>
    /// </remarks>
    public void SwitchTurn(TurnState newState)
    {
        if (currentTurnState == TurnState.GameOver) return;

        // BUG FIX: Prevent the turn manager from firing the same turn twice in a row.
        if (hasInitialized && currentTurnState == newState) return;

        hasInitialized = true;
        currentTurnState = newState;
        OnTurnChanged?.Invoke(currentTurnState);
    }

    /// <summary>
    /// A generic utility to fetch a specific combatant component from the scene hierarchy.
    /// </summary>
    /// <typeparam name="T">The specific type of component to search for (must inherit from <see cref="MonoBehaviour"/>).</typeparam>
    /// <returns>Returns the component of type <typeparamref name="T"/> if successfully located.</returns>
    /// <exception cref="System.NullReferenceException">Thrown if the required component cannot be located within the active scene.</exception>
    public T GetCombatantManager<T>() where T : MonoBehaviour
    {
        T combatant = FindAnyObjectByType<T>();

        if (combatant == null)
        {
            throw new System.NullReferenceException($"Critical Error: Component of type {typeof(T)} not found in the current scene context.");
        }

        return combatant;
    }

    /// <summary>
    /// A context menu hook allowing developers to force an enemy turn directly from the Unity Inspector during runtime testing.
    /// </summary>
    [ContextMenu("Test: Force Enemy Turn")]
    public void ForceEnemyTurn()
    {
        SwitchTurn(TurnState.EnemyTurn);
    }

    /// <summary>
    /// A parameterless helper method designed specifically for Unity Inspector Events to bypass enum limitations.
    /// </summary>
    /// <remarks>Immediately forces the combat state to the terminal GameOver phase.</remarks>
    public void TriggerGameOver()
    {
        SwitchTurn(TurnState.GameOver);
    }
}