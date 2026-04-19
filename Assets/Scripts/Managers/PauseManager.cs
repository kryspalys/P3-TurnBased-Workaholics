using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Defines standard operations for pausing and unpausing the game state.
/// </summary>
public interface IPauseManager
{
    /// <summary>
    /// Restores the game's time scale and hides the pause interface.
    /// </summary>
    void ResumeGame();

    /// <summary>
    /// Executes a transition back to the primary title screen.
    /// </summary>
    void QuitToMainMenu();
}

/// <summary>
/// Manages the runtime interruption state, time scaling, and scene escape routing.
/// </summary>
/// <remarks>
/// <para>This class utilizes the new Unity Input System to listen for interruption commands.</para>
/// <para>When triggered, it halts the physics and gameplay loops by manipulating <see cref="Time.timeScale"/>.</para>
/// <list type="bullet">
/// <item><term>Time.timeScale = 0</term><description>Freezes animations, physics, and coroutines.</description></item>
/// <item><term>Time.timeScale = 1</term><description>Restores standard game flow.</description></item>
/// </list>
/// <example>
/// Attach this to a <c>GameManager</c> object in your combat scene:
/// <code>
/// // The manager automatically listens for the Escape key in its Update loop.
/// </code>
/// </example>
/// <include file='ExternalDocs.xml' path='docs/members[@name="PauseManager"]/PauseManager/*'/>
/// </remarks>
/// <seealso cref="MainMenuManager"/>
public class PauseManager : MonoBehaviour, IPauseManager
{
    [Header("UI Dependencies")]
    /// <summary>
    /// The parent Canvas or Panel GameObject containing the pause menu UI.
    /// </summary>
    [SerializeField] private GameObject pauseCanvas;

    [Header("Scene Routing")]
    /// <summary>
    /// The string identifier of the main menu scene as registered in Build Settings.
    /// </summary>
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// Internal tracking flag representing the current interruption state.
    /// </summary>
    private bool isPaused = false;

    /// <summary>
    /// Gets the current pause state of the application.
    /// </summary>
    /// <value>A boolean indicating <c>true</c> if the game is frozen; otherwise, <c>false</c>.</value>
    public bool IsPaused => isPaused;

    /// <summary>
    /// Validates dependencies before the first frame.
    /// </summary>
    /// <exception cref="System.NullReferenceException">Thrown if the <see cref="pauseCanvas"/> is not assigned in the Inspector.</exception>
    private void Start()
    {
        if (pauseCanvas == null)
        {
            throw new System.NullReferenceException("Pause Canvas is not assigned to the PauseManager.");
        }

        // Ensure the menu is hidden and time is normal when the scene loads
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Evaluates input every frame to intercept the designated pause key.
    /// </summary>
    private void Update()
    {
        // Intercepts the Escape key using the New Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Reverses the current pause state and updates time scaling accordingly.
    /// </summary>
    private void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            pauseCanvas.SetActive(true);
            Time.timeScale = 0f; // Freeze the game
        }
        else
        {
            ResumeGame();
        }
    }

    /// <inheritdoc/>
    public void ResumeGame()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f; // Unfreeze the game
    }

    /// <inheritdoc/>
    public void QuitToMainMenu()
    {
        // Critical: Always reset time scale before loading a new scene, 
        // or the next scene will start permanently frozen!
        Time.timeScale = 1f;
        LoadSceneSafely(mainMenuSceneName);
    }

    /// <summary>
    /// Safely executes a scene transition.
    /// </summary>
    /// <param name="sceneName">The string identifier of the destination scene.</param>
    /// <exception cref="System.ArgumentException">Thrown if the <paramref name="sceneName"/> is invalid.</exception>
    private void LoadSceneSafely(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            throw new System.ArgumentException("Destination scene name cannot be null or empty.");
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// A generic utility to locate a specific component within the active Pause Canvas.
    /// </summary>
    /// <typeparam name="T">The specific UI component type to search for.</typeparam>
    /// <returns>Returns the component of type <typeparamref name="T"/> if found attached to the canvas.</returns>
    /// <remarks>Demonstrates generic fetching for isolated UI architectures.</remarks>
    public T GetPauseUIComponent<T>() where T : Component
    {
        return pauseCanvas.GetComponentInChildren<T>(true);
    }
}