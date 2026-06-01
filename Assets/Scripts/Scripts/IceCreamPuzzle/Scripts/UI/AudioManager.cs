using UnityEngine;

/// <summary>
/// Singleton AudioManager — handles BGM (per game state) and SFX.
/// Attach to a persistent GameObject in your first scene.
///
/// SETUP:
///   1. Add this component to an empty GameObject (e.g. "AudioManager").
///   2. Assign AudioClips in the Inspector.
///   3. Call AudioManager.Instance.PlaySFX_XXX() from your game scripts
///      wherever relevant events fire.
///   4. Call AudioManager.Instance.PlayMusic(MusicState.X) when game state changes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── BGM ───────────────────────────────────────────────────────────────
    [Header("Background Music")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip winMusic;

    [Range(0f, 1f)] public float musicVolume = 0.5f;

    // ── SFX ───────────────────────────────────────────────────────────────
    [Header("Sound Effects")]
    public AudioClip slideClip;
    public AudioClip levelCompleteClip;
    public AudioClip levelFailClip;
    public AudioClip uiClickClip;

    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ── Internal ──────────────────────────────────────────────────────────
    AudioSource _musicSource;
    AudioSource _sfxSource;

    public enum MusicState { Menu, Gameplay, Win }

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Two dedicated AudioSources on this GameObject
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume      = musicVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop        = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.volume      = sfxVolume;
    }

    // ── BGM ───────────────────────────────────────────────────────────────

    /// <summary>Switch BGM to match the given game state. Ignores if already playing.</summary>
    public void PlayMusic(MusicState state)
    {
        AudioClip clip = state switch
        {
            MusicState.Menu     => menuMusic,
            MusicState.Gameplay => gameplayMusic,
            MusicState.Win      => winMusic,
            _                   => null
        };

        if (clip == null || _musicSource.clip == clip) return;

        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    public void StopMusic() => _musicSource.Stop();

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        _musicSource.volume = musicVolume;
    }

    // ── SFX ───────────────────────────────────────────────────────────────

    public void PlaySFX_Slide()         => PlaySFX(slideClip);
    public void PlaySFX_LevelComplete() => PlaySFX(levelCompleteClip);
    public void PlaySFX_LevelFail()     => PlaySFX(levelFailClip);
    public void PlaySFX_UIClick()       => PlaySFX(uiClickClip);

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        _sfxSource.volume = sfxVolume;
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
