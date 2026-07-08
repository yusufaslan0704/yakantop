using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("SFX Clips")]
    public AudioClip throwSfx;
    public AudioClip hitSfx;
    public AudioClip dashSfx;
    public AudioClip reviveSfx;

    [Header("Volume")]
    [Range(0f, 1f)] public float throwVolume = 0.8f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float dashVolume = 0.7f;
    [Range(0f, 1f)] public float reviveVolume = 0.9f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayThrow()
    {
        PlaySfx(throwSfx, throwVolume);
    }

    public void PlayHit()
    {
        PlaySfx(hitSfx, hitVolume);
    }

    public void PlayDash()
    {
        PlaySfx(dashSfx, dashVolume);
    }

    public void PlayRevive()
    {
        PlaySfx(reviveSfx, reviveVolume);
    }

    void PlaySfx(AudioClip clip, float volume)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        audioSource.PlayOneShot(clip, volume);
    }
}