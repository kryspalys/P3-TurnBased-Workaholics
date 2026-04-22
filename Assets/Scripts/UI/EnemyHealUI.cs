using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A decoupled listener that visualizes the enemy's heal resource state.
/// </summary>
/// <remarks>
/// <para>Displays two potion icons that grey out as heals are consumed, a numerical counter showing
/// remaining potions, and a radial fill image representing cooldown time until the next heal.</para>
/// <para>Subscribes to <see cref="EnemyAI.OnHealUsesChanged"/> and <see cref="EnemyAI.OnHealCooldownChanged"/>
/// via Inspector wiring — no direct coupling to gameplay scripts.</para>
/// </remarks>
public class EnemyHealUI : MonoBehaviour
{
    [Header("Potion Icons")]
    /// <summary>Array of potion icon Images, ordered left-to-right. Length should match the enemy's max heal uses.</summary>
    [SerializeField] private Image[] potionIcons;

    /// <summary>Color applied to icons that still represent available heal uses.</summary>
    [SerializeField] private Color availableColor = Color.white;

    /// <summary>Color applied to icons that represent consumed heal uses (greyed out).</summary>
    [SerializeField] private Color depletedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Healing Potion Counter")]
    /// <summary>Text field displaying the numerical count of remaining potions (e.g. "2").</summary>
    [SerializeField] private TMP_Text potionCountText;

    [Header("Cooldown Indicator")]
    /// <summary>Radial fill image representing cooldown progress. Must have Image Type set to Filled.</summary>
    [SerializeField] private Image cooldownFillImage;

    /// <summary>Parent GameObject of the cooldown indicator; hidden when no cooldown is active.</summary>
    [SerializeField] private GameObject cooldownRoot;

    /// <summary>Optional text field inside the cooldown indicator showing remaining turns (e.g. "2").</summary>
    [SerializeField] private TMP_Text cooldownText;

    /// <summary>
    /// Updates the potion icon states and counter text. Wired to <see cref="EnemyAI.OnHealUsesChanged"/>.
    /// </summary>
    /// <param name="current">Heal uses still available.</param>
    /// <param name="max">Maximum possible heal uses for this encounter.</param>
    public void UpdatePotions(int current, int max)
    {
        if (potionCountText != null)
        {
            potionCountText.text = current.ToString();
        }

        if (potionIcons == null) return;

        // Grey out icons from right to left as potions are consumed.
        for (int i = 0; i < potionIcons.Length; i++)
        {
            if (potionIcons[i] == null) continue;
            potionIcons[i].color = (i < current) ? availableColor : depletedColor;
        }
    }

    /// <summary>
    /// Updates the radial cooldown fill. Wired to <see cref="EnemyAI.OnHealCooldownChanged"/>.
    /// </summary>
    /// <param name="current">Turns remaining on cooldown. Zero means heal is available.</param>
    /// <param name="max">Total cooldown duration in turns.</param>
    public void UpdateCooldown(int current, int max)
    {
        bool isOnCooldown = current > 0;

        if (cooldownRoot != null)
        {
            cooldownRoot.SetActive(isOnCooldown);
        }

        if (cooldownFillImage != null && max > 0)
        {
            // Fill drains from full to empty as the cooldown counts down.
            cooldownFillImage.fillAmount = (float)current / max;
        }

        if (cooldownText != null)
        {
            string turnWord = (current == 1) ? "turn" : "turns";
            cooldownText.text = $"{current} {turnWord}\nuntil heal";
        }
    }
}