using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A decoupled listener that updates a UI Slider based on broadcasted events.
/// </summary>
public class UIBarUpdater : MonoBehaviour
{
    /// <summary>The visual slider component to modify.</summary>
    [SerializeField] private Slider targetSlider;

    /// <summary>Caches the slider component automatically.</summary>
    private void Awake()
    {
        if (targetSlider == null)
        {
            targetSlider = GetComponent<Slider>();
        }
    }

    /// <summary>
    /// Updates the slider's current and maximum values (Used for Health).
    /// </summary>
    /// <param name="current">The current numerical value.</param>
    /// <param name="max">The maximum possible capacity.</param>
    public void UpdateBar(float current, float max)
    {
        targetSlider.maxValue = max;
        targetSlider.value = current;
    }

    /// <summary>
    /// Overloaded method to handle integer-based events (Used for the Grandma Meter).
    /// </summary>
    /// <param name="current">The current numerical value.</param>
    /// <param name="max">The maximum possible capacity.</param>
    public void UpdateBar(int current, int max)
    {
        targetSlider.maxValue = max;
        targetSlider.value = current;
    }
}