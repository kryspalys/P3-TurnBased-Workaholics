using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspector-serializable pairing of a potion's main icon with its cooldown overlay.
/// </summary>
/// <remarks>
/// Allows <see cref="EnemyHealUI"/> to treat each potion as a single unit with dual visual layers:
/// the icon shows availability via tint, while the overlay shows cooldown progress via radial fill.
/// </remarks>
[Serializable]
public class PotionSlot
{
    /// <summary>The potion sprite image. Tinted to show available/depleted state.</summary>
    public Image icon;

    /// <summary>The radial fill overlay. Active only while this specific potion is on cooldown.</summary>
    public Image cooldownOverlay;
}