using System;
using UnityEngine;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float volume = 0.42f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool startMuted;

    public bool IsMuted { get; private set; }
    public event Action<bool> MuteStateChanged;

    private void Awake()
    {
        EnsureAudioSource();
        ConfigureAudioSource();
        SetMuted(startMuted, false);
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Configure(AudioClip newMusicClip, AudioSource newAudioSource)
    {
        musicClip = newMusicClip;
        audioSource = newAudioSource;
        EnsureAudioSource();
        ConfigureAudioSource();
    }

    public void Play()
    {
        EnsureAudioSource();
        ConfigureAudioSource();

        if (audioSource == null || musicClip == null || audioSource.isPlaying)
            return;

        audioSource.Play();
    }

    public void ToggleMuted()
    {
        SetMuted(!IsMuted, true);
    }

    public void SetMuted(bool muted, bool notify = true)
    {
        IsMuted = muted;
        EnsureAudioSource();
        if (audioSource != null)
            audioSource.mute = IsMuted;

        if (notify)
            MuteStateChanged?.Invoke(IsMuted);
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
            return;

        audioSource.clip = musicClip;
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.spatialBlend = 0f;
    }
}

#pragma warning restore 0649
