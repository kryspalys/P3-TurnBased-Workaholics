using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Defines standard operations for user interface menu controllers.
/// </summary>
public interface IMenuManager
{
    /// <summary>
    /// Executes the primary start sequence for the application.
    /// </summary>
    void StartGame();

    /// <summary>
    /// Executes the termination sequence for the application.
    /// </summary>
    void QuitGame();
}

/// <summary>
/// Handles the high-level scene routing, application flow, and ambient audio for the primary title screen.
/// </summary>
/// <remarks>
/// <para>This class is responsible for bridging the UI buttons to the Unity Scene Management system and handling localized menu music.</para>
/// <para>Key responsibilities include:</para>
/// <list type="bullet">
/// <item><term>Scene Loading</term><description>Transitions the user from the menu to the gameplay state.</description></item>
/// <item><term>Application Exit</term><description>Safely shuts down the executable.</description></item>
/// <item><term>Audio Management</term><description>Plays background music exclusively during the menu lifecycle.</description></item>
/// </list>
/// <example>
/// Attach this script to an empty <c>MenuManager</c> GameObject and link the UI Button OnClick events:
/// <code>
/// // Example of internal usage:
/// myMenuManager.StartGame();
/// </code>
/// </example>
/// </remarks>
public class MainMenuManager : MonoBehaviour, IMenuManager
{
    [Header("Scene Routing")]
    /// <summary>
    /// The exact string identifier of the target scene to load.
    /// </summary>
    [SerializeField] private string targetSceneName = "CombatScene";

    [Header("Audio Settings")]
    /// <summary>
    /// The looping background music track to play while the menu is active.
    /// </summary>
    [SerializeField] private AudioClip menuMusic;

    /// <summary>
    /// Master volume multiplier applied to the menu music.
    /// </summary>
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    /// <summary>
    /// Internal dedicated AudioSource for background music, generated dynamically at runtime.
    /// </summary>
    private AudioSource musicSource;

    /// <summary>
    /// Gets the name of the designated combat scene.
    /// </summary>
    /// <value>A <see cref="string"/> representing the scene file name as registered in the Build Settings.</value>
    public string TargetSceneName => targetSceneName;

    /// <summary>
    /// Constructs the internal audio source component and applies baseline configuration.
    /// </summary>
    private void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f; // Ensures standard 2D audio with no positional falloff
    }

    /// <summary>
    /// Initiates ambient audio playback after all initializations have resolved.
    /// </summary>
    private void Start()
    {
        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    /// <inheritdoc/>
    public void StartGame()
    {
        StopMusicSafely();
        LoadSceneTransition(targetSceneName);
    }

    /// <inheritdoc/>
    public void QuitGame()
    {
        Debug.Log("Quit command registered.");
        StopMusicSafely();

#if UNITY_EDITOR
        // This stops the play mode inside the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // This closes the actual executable once the game is built
        Application.Quit();
#endif
    }

    /// <summary>
    /// Safely halts audio playback if an active track is currently playing.
    /// </summary>
    private void StopMusicSafely()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Validates and loads a specific scene by its string identifier.
    /// </summary>
    /// <param name="sceneName">The string name of the scene to load.</param>
    /// <returns>Returns <c>true</c> if the scene load command was successfully issued; otherwise, throws an exception.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the <paramref name="sceneName"/> is null or empty.</exception>
    /// <remarks>
    /// This method wraps <see cref="SceneManager.LoadScene(string)"/> to provide safety checks before execution.
    /// </remarks>
    private bool LoadSceneTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            throw new System.ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));
        }

        SceneManager.LoadScene(sceneName);
        return true;
    }

    /// <summary>
    /// A generic utility to locate specific UI components in the menu scene.
    /// </summary>
    /// <typeparam name="T">The type of UI component to locate (e.g., UnityEngine.UI.Button).</typeparam>
    /// <returns>An array of all active components of type <typeparamref name="T"/> in the scene.</returns>
    /// <remarks>
    /// This method demonstrates generic type fetching within the menu architecture.
    /// </remarks>
    public T[] FindMenuElements<T>() where T : Component
    {
        return FindObjectsByType<T>(FindObjectsSortMode.None);
    }
}