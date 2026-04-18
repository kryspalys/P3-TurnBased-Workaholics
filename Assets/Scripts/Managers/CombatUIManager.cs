using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatSettings;

/// <summary>
/// Handles all visual representations of the combat state.
/// </summary>
/// <remarks>
/// This class acts purely as a listener. It contains no gameplay logic or damage calculations.
/// </remarks>
public class CombatUIManager : MonoBehaviour
{
    [Header("Turn UI")]
    /// <summary>Text field displaying whose turn is active.</summary>
    [SerializeField] private TMP_Text turnIndicatorText;
    /// <summary>Text field displaying the AI's programmatic intent.</summary>
    [SerializeField] private TMP_Text aiActionLogText;
    /// <summary>Array containing all player action buttons to manage interactability states.</summary>
    [SerializeField] private Button[] playerActionButtons;

    [Header("Game Over UI")]
    /// <summary>The parent canvas panel containing the terminal screen elements.</summary>
    [SerializeField] private GameObject gameOverPanel;
    /// <summary>Text field displaying the winner of the bout.</summary>
    [SerializeField] private TMP_Text gameOverWinnerText;

    /// <summary>
    /// Translates the logical <see cref="TurnState"/> into visual HUD updates and manages button interaction.
    /// </summary>
    /// <param name="currentState">The newly active turn state passed from the event broadcast.</param>
    public void UpdateTurnIndicator(TurnState currentState)
    {
        switch (currentState)
        {
            case TurnState.PlayerTurn:
                turnIndicatorText.text = "Grandma's Turn";
                aiActionLogText.text = "Waiting for your move...";
                SetPlayerButtonsInteractable(true);
                break;
            case TurnState.EnemyTurn:
                turnIndicatorText.text = "Enemy's Turn";
                SetPlayerButtonsInteractable(false);
                break;
            case TurnState.GameOver:
                SetPlayerButtonsInteractable(false);
                break;
        }
    }

    /// <summary>
    /// Updates the on-screen combat log with the AI's chosen action.
    /// </summary>
    /// <param name="message">The string representation of the AI's intent.</param>
    public void UpdateAILog(string message)
    {
        aiActionLogText.text = message;
    }

    /// <summary>
    /// Iterates through all combat buttons and toggles their active state.
    /// </summary>
    /// <param name="state">True to enable clicking; false to visually disable the buttons.</param>
    private void SetPlayerButtonsInteractable(bool state)
    {
        foreach (Button btn in playerActionButtons)
        {
            btn.interactable = state;
        }
    }

    /// <summary>
    /// Activates the terminal screen and broadcasts the winner.
    /// </summary>
    /// <param name="didPlayerWin">True if the player survived; false if the AI survived.</param>
    public void TriggerGameOver(bool didPlayerWin)
    {
        gameOverPanel.SetActive(true);
        gameOverWinnerText.text = didPlayerWin ? "Grandma Survived!" : "The Beast Wins...";
    }
}