using System;
using UnityEngine;

/// <summary>
/// Maps each <see cref="SfxId"/> to its clips and playback settings. Audio data, edited by whoever owns the sound
/// (Han); the AudioService only reads it. Several clips in one entry are alternatives picked at random.
/// </summary>
[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Overtime/Audio Catalog")]
public class AudioCatalogSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch variation around 1 (0.05 = +-5%), so a repeated sound does not feel mechanical")]
        [Range(0f, 0.5f)] public float pitchJitter;
        [Tooltip("Seconds that must pass before the same id can play again (stops machine-gun repeats)")]
        [Min(0f)] public float minInterval;
        [Tooltip("Keep playing while the game is frozen (game over, modal panels): stingers and end-of-run sounds")]
        public bool ignoreListenerPause;
    }

    [Header("Money tiers (MoneyService.OnIncome)")]
    [Tooltip("An income of at most this much plays MoneyGainSmall")]
    public int moneySmallMax = 10;
    [Tooltip("An income of at most this much plays MoneyGainMedium; more plays MoneyGainLarge")]
    public int moneyMediumMax = 40;

    public Entry[] entries;
}
