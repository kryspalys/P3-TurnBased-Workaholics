namespace CombatSettings
{
    /// <summary>
    /// Represents the distinct phases of the combat loop architecture.
    /// </summary>
    /// <remarks>
    /// <para>This enumeration serves as the central authority for the game's state machine.</para>
    /// <para>Rules for state transitions and their effects:</para>
    /// <list type="bullet">
    /// <item><term>PlayerTurn</term><description>Allows player UI input; ignores background AI processing.</description></item>
    /// <item><term>EnemyTurn</term><description>Locks player UI input; triggers programmatic AI coroutines.</description></item>
    /// <item><term>GameOver</term><description>Terminal state; locks all inputs, halts gameplay, and triggers final UI screens.</description></item>
    /// </list>
    /// <example>
    /// <code>
    /// // Standard evaluation check before permitting an action:
    /// if (currentTurn == TurnState.PlayerTurn) 
    /// { 
    ///     ExecutePlayerAction(); 
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="TurnManager"/>
    public enum TurnState
    {
        /// <summary>The phase where the protagonist selects an action from the interface.</summary>
        PlayerTurn,

        /// <summary>The phase where the antagonist evaluates conditions and executes a response.</summary>
        EnemyTurn,

        /// <summary>The terminal phase triggered when a combatant's hit points reach zero.</summary>
        GameOver
    }
}