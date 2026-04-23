using System;
using UnityEngine;

/// <summary>
/// Inspector-serializable pairing of an audio clip with its playback parameters.
/// </summary>
/// <remarks>
/// Used by <see cref="AudioManager"/> to configure individual SFX without requiring
/// per-clip AudioSource components in the scene hierarchy.
/// </remarks>
[Serializable]
public class SoundEntry
{
    /// <summary>The audio clip asset to play for this entry.</summary>
    public AudioClip clip;

    /// <summary>Playback volume for this clip, scaled against the manager's master SFX volume.</summary>
    [Range(0f, 1f)] public float volume = 1f;

    /// <summary>Pitch multiplier. Set below 1.0 for deeper sounds, above for higher.</summary>
    [Range(0.5f, 2f)] public float pitch = 1f;

    /// <summary>Random pitch variance applied per play, for natural variation on repeated SFX.</summary>
    [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
}