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
    public AudioClip dodgeSfx;

    [Header("Volume")]
    [Range(0f, 1f)] public float throwVolume = 0.8f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float dashVolume = 0.7f;
    [Range(0f, 1f)] public float reviveVolume = 0.9f;
    [Range(0f, 1f)] public float dodgeVolume = 0.75f;

    [Header("Variation")]
    [Tooltip("Her ses biraz farklı tonda çalar, tekrar hissini kırar. 0 = kapalı.")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.08f;

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

    // Full charge hazir blip — ayni clip, daha tiz / kisa.
    public void PlayThrowChargeReady()
    {
        if (audioSource == null || throwSfx == null)
        {
            return;
        }

        float prev = audioSource.pitch;
        audioSource.pitch = Random.Range(1.18f, 1.32f);
        audioSource.PlayOneShot(throwSfx, throwVolume * 0.35f);
        audioSource.pitch = prev;
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

    public void PlayDodge()
    {
        if (dodgeSfx != null)
        {
            PlaySfx(dodgeSfx, dodgeVolume);
            return;
        }

        // Dodge sesi yoksa dash sesine dus.
        PlayDash();
    }

    // Top tipine özel sesler gibi dışarıdan gelen klipler için.
    public void PlayClip(AudioClip clip, float volume)
    {
        PlaySfx(clip, volume);
    }

    void PlaySfx(AudioClip clip, float volume)
    {
        if (audioSource == null) return;
        if (clip == null) return;

        audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);

        audioSource.PlayOneShot(clip, volume);
    }
}