using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;

    public bool IsMuted { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (IsMuted || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void SetMute(bool mute)
    {
        IsMuted = mute;
    }

    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }
}
