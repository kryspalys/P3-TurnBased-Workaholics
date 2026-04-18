namespace CombatSettings
{
    /// <summary>
    /// Represents the distinct phases of the combat loop.
    /// </summary>
    /// <remarks>
    /// <para>This enumeration is the central authority for the game's state machine.</para>
    /// <para>Rules for state transitions:</para>
    /// <list type="bullet">
    /// <item><term>PlayerTurn</term><description>Allows player input; ignores AI logic.</description></item>
    /// <item><term>EnemyTurn</term><description>Locks player input; triggers AI coroutines.</description></item>
    /// <item><term>GameOver</term><description>Terminal state; locks all inputs and halts gameplay.</description></item>
    /// </list>
    /// <example>
    /// <code>
    /// if (currentTurn == TurnState.PlayerTurn) { /* Allow UI interaction */ }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="TurnManager"/>
    public enum TurnState
    {
        /// <summary>The state where Grandma (the player) selects an action.</summary>
        PlayerTurn,
        /// <summary>The state where the Big Bad Wolf (the AI) executes a programmed response.</summary>
        EnemyTurn,
        /// <summary>The terminal state triggered when either combatant's health reaches zero.</summary>
        GameOver
    }
}