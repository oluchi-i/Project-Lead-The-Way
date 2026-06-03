using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UIButtonClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.85f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }

    public void Configure(AudioClip newClickSound, AudioSource newAudioSource)
    {
        clickSound = newClickSound;
        audioSource = newAudioSource;
    }

    private void PlayClick()
    {
        if (clickSound == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clickSound, volume);
    }
}
