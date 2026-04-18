using UnityEngine;
using UnityEngine.Events;
using CombatSettings;

/// <summary>
/// The central controller that manages the flow of <see cref="TurnState"/> transitions.
/// </summary>
/// <remarks>
/// <para>This class enforces the strict turn order required by the game architecture. 
/// No other script is permitted to change the turn state directly; they must request a change through this manager.</para>
/// <include file='ExternalDocs.xml' path='docs/members[@name="TurnManager"]/TurnManager/*'/>
/// </remarks>
public class TurnManager : MonoBehaviour
{
    /// <summary>
    /// The current operational state of the combat loop.
    /// </summary>
    [SerializeField] private TurnState currentTurnState = TurnState.PlayerTurn;

    /// <summary>
    /// Broadcasts an event containing the new <see cref="TurnState"/> whenever the state changes.
    /// </summary>
    [Header("Broadcasting Events")]
    public UnityEvent<TurnState> OnTurnChanged;

    /// <summary>
    /// Gets the current active turn state.
    /// </summary>
    /// <value>Returns the current <see cref="TurnState"/> enumeration.</value>
    public TurnState CurrentTurn => currentTurnState;

    /// <summary>
    /// Initializes the combat loop by forcing the initial state broadcast.
    /// </summary>
    private void Start()
    {
        SwitchTurn(TurnState.PlayerTurn);
    }

    /// <summary>
    /// Transitions the game to a new turn state and broadcasts the change.
    /// </summary>
    /// <param name="newState">The specific <see cref="TurnState"/> to transition into.</param>
    /// <remarks>
    /// If the <paramref name="newState"/> or the current state is <c>TurnState.GameOver</c>, further transitions are blocked.
    /// </remarks>
    public void SwitchTurn(TurnState newState)
    {
        if (currentTurnState == TurnState.GameOver) return;

        currentTurnState = newState;
        OnTurnChanged?.Invoke(currentTurnState);
    }

    /// <summary>
    /// A generic utility to fetch a specific combatant component from the scene, demonstrating generic type documentation.
    /// </summary>
    /// <typeparam name="T">The type of component to search for (must inherit from <see cref="MonoBehaviour"/>).</typeparam>
    /// <returns>Returns the component of type <typeparamref name="T"/> if found; otherwise, null.</returns>
    /// <exception cref="System.NullReferenceException">Thrown if the required component cannot be located.</exception>
    public T GetCombatantManager<T>() where T : MonoBehaviour
    {
        T combatant = FindAnyObjectByType<T>();
        if (combatant == null) throw new System.NullReferenceException($"Component of type {typeof(T)} not found.");
        return combatant;
    }

    /// <summary>
    /// Context menu hook for forcing the enemy turn directly from the Unity Inspector during testing.
    /// </summary>
    [ContextMenu("Test: Force Enemy Turn")]
    public void ForceEnemyTurn()
    {
        SwitchTurn(TurnState.EnemyTurn);
    }
}