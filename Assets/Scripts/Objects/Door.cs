using UnityEngine;
using UnityEngine.Serialization;

namespace DoorScript
{
    [RequireComponent(typeof(AudioSource))]
    public class Door : MonoBehaviour
    {
        [SerializeField] public bool open;
        [SerializeField] public float smooth = 1f;
        [FormerlySerializedAs("DoorOpenAngle")]
        [SerializeField] private float doorOpenAngle = -90f;
        [FormerlySerializedAs("DoorCloseAngle")]
        [SerializeField] private float doorCloseAngle = 0f;
        [SerializeField] public AudioSource asource;
        [SerializeField] public AudioClip openDoor;
        [SerializeField] public AudioClip closeDoor;
        [SerializeField, Range(0f, 2f)] public float soundVolume = 1.5f;
        [SerializeField, Range(0f, 1f)] public float spatialBlend = 0.2f;

        private bool isAnimating;

        public bool IsAnimating => isAnimating;

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void Start()
        {
            transform.localRotation = GetTargetRotation();
        }

        private void Update()
        {
            if (!isAnimating)
                return;

            var target = GetTargetRotation();
            var degreesPerSecond = Mathf.Max(1f, smooth * 450f);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, degreesPerSecond * Time.deltaTime);

            if (Quaternion.Angle(transform.localRotation, target) > 0.2f)
                return;

            transform.localRotation = target;
            isAnimating = false;
        }

        public void OpenDoor()
        {
            SetOpen(!open);
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void SetOpen(bool shouldOpen)
        {
            if (open == shouldOpen && !isAnimating)
                return;

            open = shouldOpen;
            isAnimating = true;

            var clip = open ? openDoor : closeDoor;
            EnsureAudioSource();
            if (clip != null && asource != null)
                asource.PlayOneShot(clip, soundVolume);
        }

        private Quaternion GetTargetRotation()
        {
            return Quaternion.Euler(0f, open ? doorOpenAngle : doorCloseAngle, 0f);
        }

        private void EnsureAudioSource()
        {
            if (asource == null)
                asource = GetComponent<AudioSource>();

            if (asource == null)
                return;

            asource.playOnAwake = false;
            asource.volume = Mathf.Clamp01(soundVolume);
            asource.spatialBlend = spatialBlend;
        }
    }
}
