using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A decoupled listener that visualizes the enemy's heal resource state.
/// </summary>
/// <remarks>
/// <para>Each potion slot is a two-layer UI element: the icon shows availability, and a radial overlay
/// drains to show cooldown progress on the potion that was most recently consumed.</para>
/// <para>Subscribes to <see cref="EnemyAI.OnHealUsesChanged"/> and <see cref="EnemyAI.OnHealCooldownChanged"/>
/// via Inspector wiring — no direct coupling to gameplay scripts.</para>
/// </remarks>
public class EnemyHealUI : MonoBehaviour
{
    [Header("Potion Slots")]
    /// <summary>Array of potion slots, ordered left-to-right. Length should match the enemy's max heal uses.</summary>
    [SerializeField] private PotionSlot[] potionSlots;

    /// <summary>Color applied to icons that still represent available heal uses.</summary>
    [SerializeField] private Color availableColor = Color.white;

    /// <summary>Color applied to icons that represent consumed heal uses (greyed out).</summary>
    [SerializeField] private Color depletedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    
    [Header("Potion Counter")]
    /// <summary>Text field displaying the numerical count of remaining potions.</summary>
    [SerializeField] private TMP_Text potionCountText;
   
    /// <summary>Text field displaying the cooldown status in plain language (e.g. "2 turns until next heal").</summary>
    [SerializeField] private TMP_Text cooldownText;

    /// <summary>Cached count of remaining potions, needed to determine which slot is "on cooldown".</summary>
    private int lastKnownPotionCount;

    /// <summary>
    /// Updates the potion icon tints and hides all cooldown overlays.
    /// Wired to <see cref="EnemyAI.OnHealUsesChanged"/>.
    /// </summary>
    /// <param name="current">Heal uses still available.</param>
    /// <param name="max">Maximum possible heal uses for this encounter.</param>
    public void UpdatePotions(int current, int max)
    {
        lastKnownPotionCount = current;

        if (potionCountText != null)
        {
            potionCountText.text = current.ToString();
        }

        if (potionSlots == null) return;

        // Icons to the left of `current` are available; the rest are depleted.
        // Overlays start hidden — they'll be turned on by UpdateCooldown if a heal just fired.
        for (int i = 0; i < potionSlots.Length; i++)
        {
            if (potionSlots[i] == null || potionSlots[i].icon == null) continue;

            potionSlots[i].icon.color = (i < current) ? availableColor : depletedColor;

            if (potionSlots[i].cooldownOverlay != null)
            {
                potionSlots[i].cooldownOverlay.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Updates the cooldown overlay on the most recently consumed potion slot.
    /// Wired to <see cref="EnemyAI.OnHealCooldownChanged"/>.
    /// </summary>
    /// <param name="current">Turns remaining on cooldown. Zero means heal is available.</param>
    /// <param name="max">Total cooldown duration in turns.</param>
    public void UpdateCooldown(int current, int max)
    {
        if (potionSlots == null || potionSlots.Length == 0) return;

        // The "just consumed" slot is the first depleted one from the left.
        // e.g. if 2 potions start and 1 is left, slot index 1 is on cooldown.
        int cooldownSlotIndex = lastKnownPotionCount;

        for (int i = 0; i < potionSlots.Length; i++)
        {
            if (potionSlots[i] == null || potionSlots[i].cooldownOverlay == null) continue;

            bool isThisSlotOnCooldown = (i == cooldownSlotIndex) && (current > 0);
            potionSlots[i].cooldownOverlay.gameObject.SetActive(isThisSlotOnCooldown);

            if (isThisSlotOnCooldown && max > 0)
            {
                potionSlots[i].cooldownOverlay.fillAmount = (float)current / max;
            }
        }
          // Status label: visible only while cooldown is active, cleared when heal is available.
        if (cooldownText != null)
            {
                if (current > 0)
                {
                    // Grammar: "1 turn" singular, "2 turns" / "3 turns" plural.
                    string turnWord = (current == 1) ? "turn" : "turns";
                    cooldownText.text = $"{current} {turnWord} until next heal";
                }
                else
                {
                    cooldownText.text = string.Empty;
                }
            }
    }
}