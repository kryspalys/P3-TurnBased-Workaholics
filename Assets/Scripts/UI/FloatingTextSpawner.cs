using UnityEngine;

/// <summary>
/// Defines the standard contract for dynamic visual text generation.
/// </summary>
public interface ITextSpawner
{
    /// <summary>
    /// Instantiates the visual text element based on combat mathematical data.
    /// </summary>
    /// <param name="damageAmount">The numerical damage to display.</param>
    /// <param name="isCrit">A boolean flag determining if critical formatting applies.</param>
    void SpawnFloatingText(float damageAmount, bool isCrit);

    /// <summary>
    /// Instantiates the visual text element specifically styled for restorative actions.
    /// </summary>
    /// <param name="healAmount">The numerical amount of health restored.</param>
    void SpawnHealText(float healAmount);
}

/// <summary>
/// Automatically listens to a local <see cref="Health"/> component and instantiates floating text upon taking damage or healing.
/// </summary>
/// <remarks>
/// <para>Attach this script directly to any character GameObject (Grandma or Wolf) that also contains a health component.</para>
/// <include file='ExternalDocs.xml' path='docs/members[@name="FloatingTextSpawner"]/FloatingTextSpawner/*'/>
/// </remarks>
/// <seealso cref="DamagePopup"/>
[RequireComponent(typeof(Health))]
public class FloatingTextSpawner : MonoBehaviour, ITextSpawner
{
    [Header("Dependencies")]
    /// <summary>The designated prefab object containing the TextMeshPro and layout logic.</summary>
    [SerializeField] private DamagePopup damagePopupPrefab;

    /// <summary>The specific transform coordinate where the text should originate (e.g., above the character's head).</summary>
    [SerializeField] private Transform spawnPoint;

    /// <summary>Internal reference to the required sibling health component.</summary>
    private Health healthComponent;

    /// <summary>
    /// Locates and caches the sibling health component, validating prefab assignments.
    /// </summary>
    /// <exception cref="System.NullReferenceException">Thrown if the required prefab is not assigned in the Unity Inspector.</exception>
    private void Awake()
    {
        if (damagePopupPrefab == null)
        {
            throw new System.NullReferenceException("Critical Error: DamagePopup Prefab is missing from the spawner component.");
        }

        healthComponent = GetComponent<Health>();
    }

    /// <summary>
    /// Subscribes the spawner to the local health component's damage and healing broadcasts.
    /// </summary>
    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamageTaken.AddListener(SpawnFloatingText);
            healthComponent.OnHealed.AddListener(SpawnHealText);
        }
    }

    /// <summary>
    /// Unsubscribes from the health component to prevent memory degradation and ghost execution.
    /// </summary>
    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamageTaken.RemoveListener(SpawnFloatingText);
            healthComponent.OnHealed.RemoveListener(SpawnHealText);
        }
    }

    /// <inheritdoc/>
    public void SpawnFloatingText(float damageAmount, bool isCrit)
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        DamagePopup popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        popup.Setup(damageAmount, isCrit);
    }

    /// <inheritdoc/>
    public void SpawnHealText(float healAmount)
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        DamagePopup popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        popup.SetupHeal(healAmount);
    }

    /// <summary>
    /// A generic utility demonstrating dynamic prefab generation and specific component extraction.
    /// </summary>
    /// <typeparam name="T">The specific component type to retrieve from the newly instantiated prefab.</typeparam>
    /// <param name="prefab">The source <see cref="GameObject"/> to clone into the active scene.</param>
    /// <returns>Returns the component of type <typeparamref name="T"/> attached to the cloned object.</returns>
    public T InstantiateAndGet<T>(GameObject prefab) where T : Component
    {
        GameObject clone = Instantiate(prefab);
        return clone.GetComponent<T>();
    }
}